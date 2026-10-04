namespace Conde.Memoria;

/// <summary>
/// O sorteio com semente, escrito por extenso: o Random do .NET nao promete a
/// mesma sequencia entre versoes nem entre sistemas, e a CI roda em tres.
/// </summary>
public sealed class Aleatorio
{
    private ulong estado;

    public Aleatorio(ulong semente) => estado = semente * 2 + 1;

    public ulong Proximo()
    {
        estado = unchecked(estado * 6364136223846793005UL + 1442695040888963407UL);
        return estado >> 17;
    }

    public int Entre(int baixo, int alto) => baixo + (int)(Proximo() % (ulong)(alto - baixo + 1));

    public bool Chance(int porcento) => Entre(1, 100) <= porcento;
}

/// <summary>
/// As CARGAS: montes montados para parecer o que programa de verdade faz com a
/// memoria.
///
/// Elas existem porque fragmentacao, vazamento e custo de coleta nao sao
/// propriedades do coletor: sao propriedades do PAR coletor e carga. Um coletor
/// que ganha numa carga perde em outra, e uma tabela com uma carga so engana.
/// </summary>
public static class Cargas
{
    /// <summary>
    /// LISTA: uma cadeia longa, com a raiz na ponta.
    ///
    /// Ela e o caso que quebra coletor recursivo: a marcacao desce um nivel por
    /// objeto, e com cem mil objetos a pilha estoura. E tambem o pior caso do
    /// geracional, porque a lista inteira esta viva e nada morre jovem.
    /// </summary>
    public static Monte Lista(int quantos, int coleta = 0)
    {
        var monte = new Monte();
        var anterior = -1;

        for (var i = 0; i < quantos; i++)
        {
            var objeto = monte.Criar(16, 1, coleta);
            if (anterior >= 0) monte.Apontar(anterior, 0, objeto.Id);
            else monte.Raizes.Add(objeto.Id);
            anterior = objeto.Id;
        }

        return monte;
    }

    /// <summary>
    /// ARVORE: um objeto com dois filhos, recursivamente.
    ///
    /// E a carga que mais parece com estrutura de programa de verdade, e a que
    /// o coletor copiador gosta: os sobreviventes saem compactos no destino.
    /// </summary>
    public static Monte Arvore(int fundura, int coleta = 0)
    {
        var monte = new Monte();
        var raiz = Ramo(monte, fundura, coleta);
        monte.Raizes.Add(raiz);
        return monte;
    }

    private static int Ramo(Monte monte, int fundura, int coleta)
    {
        var objeto = monte.Criar(24, 2, coleta);
        if (fundura <= 1) return objeto.Id;
        monte.Apontar(objeto.Id, 0, Ramo(monte, fundura - 1, coleta));
        monte.Apontar(objeto.Id, 1, Ramo(monte, fundura - 1, coleta));
        return objeto.Id;
    }

    /// <summary>
    /// CICLOS: pares de objetos apontando um para o outro, sem raiz nenhuma.
    ///
    /// E a carga que a contagem de referencia nao consegue limpar, e ela existe
    /// para a medida mostrar o tamanho do vazamento em vez de citar a frase.
    /// </summary>
    public static Monte Ciclos(int pares, int coleta = 0)
    {
        var monte = new Monte();
        for (var i = 0; i < pares; i++)
        {
            var a = monte.Criar(16, 1, coleta);
            var b = monte.Criar(16, 1, coleta);
            monte.Apontar(a.Id, 0, b.Id);
            monte.Apontar(b.Id, 0, a.Id);
        }
        return monte;
    }

    /// <summary>
    /// MISTURA: objetos sorteados, a maioria efemera, alguns ligados em ciclo, e
    /// algumas raizes.
    ///
    /// E a carga mais parecida com um programa real: muito objeto que morre
    /// logo, uma estrutura que sobrevive, e ciclo acontecendo sem ninguem
    /// querer.
    /// </summary>
    public static Monte Mistura(int quantos, ulong semente, int coleta = 0, int chanceDeCiclo = 10)
    {
        var sorte = new Aleatorio(semente);
        var monte = new Monte();
        var ids = new List<int>();

        for (var i = 0; i < quantos; i++)
        {
            var objeto = monte.Criar(sorte.Entre(8, 64), sorte.Entre(0, 2), coleta);
            ids.Add(objeto.Id);

            for (var campo = 0; campo < objeto.Campos.Length && ids.Count > 1; campo++)
            {
                var alvo = ids[sorte.Entre(0, ids.Count - 2)];
                monte.Apontar(objeto.Id, campo, alvo);

                // Fechar o ciclo de volta, as vezes.
                if (sorte.Chance(chanceDeCiclo) && monte[alvo].Campos.Length > 0)
                    monte.Apontar(alvo, 0, objeto.Id);
            }
        }

        var raizes = Math.Max(1, quantos / 50);
        for (var i = 0; i < raizes; i++) monte.Raizes.Add(ids[sorte.Entre(0, ids.Count - 1)]);

        return monte;
    }

    /// <summary>
    /// A carga do coletor GERACIONAL: um monte velho estavel mais uma leva de
    /// objetos novos, a maioria dos quais morre na hora.
    /// </summary>
    public static Monte Geracoes(int velhos, int novos, int sobrevivem, ulong semente)
    {
        var sorte = new Aleatorio(semente);
        var monte = new Monte();

        var cadeia = new List<int>();
        for (var i = 0; i < velhos; i++)
        {
            var objeto = monte.Criar(32, 2, 0);
            cadeia.Add(objeto.Id);
            if (i > 0) monte.Apontar(cadeia[i - 1], 0, objeto.Id);
        }
        if (cadeia.Count > 0) monte.Raizes.Add(cadeia[0]);

        for (var i = 0; i < novos; i++)
        {
            var objeto = monte.Criar(16, 1, 1);

            // Os que sobrevivem ficam pendurados num velho, que e exatamente o
            // ponteiro de velho para novo que o conjunto lembrado existe para
            // enxergar.
            if (i < sobrevivem && cadeia.Count > 0)
                monte.Apontar(cadeia[sorte.Entre(0, cadeia.Count - 1)], 1, objeto.Id);
        }

        return monte;
    }

    /// <summary>As cargas, com nome, para as varreduras.</summary>
    public static IEnumerable<(string Nome, Func<Monte> Montar)> Todas()
    {
        yield return ("lista de 2.000", () => Lista(2000));
        yield return ("arvore de 2^12", () => Arvore(12));
        yield return ("500 ciclos", () => Ciclos(500));
        yield return ("mistura de 2.000", () => Mistura(2000, 7));
        yield return ("geracoes", () => Geracoes(500, 1500, 60, 9));
    }

    /// <summary>Os coletores, com nome, para as varreduras.</summary>
    public static IEnumerable<IColetor> Coletores()
    {
        yield return new MarcaVarre();
        yield return new Copiador();
        yield return new Contagem();
    }
}
