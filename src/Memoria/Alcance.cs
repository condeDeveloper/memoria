namespace Conde.Memoria;

/// <summary>
/// O JUIZ: o que da para alcancar a partir das raizes.
///
/// Esta e a definicao de LIXO, e ela nao tem nada de aproximado: um objeto e
/// lixo quando o programa nao tem mais como chegar nele, por nenhum caminho de
/// ponteiros. Nao importa se alguem ainda aponta para ele, nao importa se o
/// contador dele e alto, nao importa se ele foi usado agorinha.
///
/// Todo coletor deste repositorio e conferido contra esta funcao, e a conferencia
/// tem DOIS lados que precisam valer juntos:
///
///   1. nada alcancavel foi apagado  (se falhar, o programa quebra)
///   2. nada inalcancavel sobrou     (se falhar, a memoria vaza)
///
/// Um coletor que nunca apaga nada passa no primeiro e falha no segundo; um que
/// apaga tudo passa no segundo e falha no primeiro. So os dois juntos dizem
/// alguma coisa.
/// </summary>
public static class Alcance
{
    /// <summary>
    /// Os objetos alcancaveis a partir das raizes, por busca em largura.
    ///
    /// Largura e nao profundidade por um motivo pratico: um monte com uma lista
    /// encadeada de cem mil objetos estoura a pilha numa busca recursiva, e
    /// lista encadeada longa e exatamente o que um monte de verdade tem.
    /// </summary>
    public static HashSet<int> Vivos(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);

        var vivos = new HashSet<int>();
        var fila = new Queue<int>();

        foreach (var raiz in monte.Raizes)
            if (monte.Existe(raiz) && vivos.Add(raiz)) fila.Enqueue(raiz);

        while (fila.Count != 0)
        {
            var atual = fila.Dequeue();
            foreach (var apontado in monte[atual].Apontados)
                if (monte.Existe(apontado) && vivos.Add(apontado)) fila.Enqueue(apontado);
        }

        return vivos;
    }

    /// <summary>Os objetos que o programa nao alcanca mais.</summary>
    public static HashSet<int> Lixo(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        var vivos = Vivos(monte);
        return [.. monte.Objetos.Keys.Where(id => !vivos.Contains(id))];
    }

    /// <summary>O veredito sobre um monte depois de uma coleta.</summary>
    public sealed class Veredito
    {
        public Veredito(IReadOnlyCollection<int> perdidos, IReadOnlyCollection<int> vazados)
        {
            Perdidos = perdidos;
            Vazados = vazados;
        }

        /// <summary>Alcancaveis que o coletor apagou. E o erro grave.</summary>
        public IReadOnlyCollection<int> Perdidos { get; }

        /// <summary>Inalcancaveis que o coletor deixou. E o vazamento.</summary>
        public IReadOnlyCollection<int> Vazados { get; }

        public bool Correto => Perdidos.Count == 0;
        public bool Completo => Vazados.Count == 0;

        public override string ToString() =>
            Correto && Completo ? "correto e completo"
            : !Correto ? $"APAGOU {Perdidos.Count} objetos alcancaveis: [{string.Join(",", Perdidos.Take(5))}]"
            : $"vazou {Vazados.Count} objetos inalcancaveis: [{string.Join(",", Vazados.Take(5))}]";
    }

    /// <summary>
    /// Confere um monte DEPOIS da coleta contra o que ele era ANTES.
    ///
    /// Precisa dos dois porque a pergunta "o coletor apagou alguma coisa viva?"
    /// so tem resposta olhando quem estava vivo antes. Depois da coleta, um
    /// objeto que foi apagado por engano simplesmente nao existe, e o monte
    /// resultante parece perfeitamente consistente.
    /// </summary>
    public static Veredito Conferir(Monte antes, Monte depois)
    {
        ArgumentNullException.ThrowIfNull(antes);
        ArgumentNullException.ThrowIfNull(depois);

        var deviamViver = Vivos(antes);
        var perdidos = deviamViver.Where(id => !depois.Existe(id)).ToList();
        var vazados = depois.Objetos.Keys.Where(id => !deviamViver.Contains(id)).ToList();

        return new Veredito(perdidos, vazados);
    }

    /// <summary>
    /// Confere tambem que os PONTEIROS continuam certos depois da coleta.
    ///
    /// Um coletor que copia objetos precisa consertar todo ponteiro que apontava
    /// para o lugar antigo, e esquecer um deles produz um monte em que os
    /// objetos certos existem e as ligacoes estao trocadas. A contagem de
    /// sobreviventes nao ve isso.
    /// </summary>
    public static bool LigacoesIntactas(Monte antes, Monte depois)
    {
        ArgumentNullException.ThrowIfNull(antes);
        ArgumentNullException.ThrowIfNull(depois);

        foreach (var id in Vivos(antes))
        {
            if (!depois.Existe(id)) return false;

            var velho = antes[id];
            var novo = depois[id];
            if (velho.Campos.Length != novo.Campos.Length) return false;

            for (var campo = 0; campo < velho.Campos.Length; campo++)
            {
                var alvo = velho.Campos[campo];
                if (alvo < 0 || !antes.Existe(alvo)) continue;
                if (novo.Campos[campo] != alvo) return false;
            }
        }

        return true;
    }
}
