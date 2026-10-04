namespace Conde.Memoria;

/// <summary>
/// O coletor GERACIONAL: ele coleta so os objetos NOVOS, e passa longe dos
/// velhos.
///
/// A aposta e a HIPOTESE GERACIONAL: a maioria dos objetos morre jovem. Se isso
/// for verdade, coletar so o pedaco novo do monte acha quase todo o lixo com
/// uma fracao do trabalho.
///
/// Este repositorio nao supoe a hipotese: ele a MEDE, carga por carga, e mostra
/// em quais ela vale e em quais nao vale.
///
/// E ha uma armadilha que faz o coletor geracional ser difícil de escrever
/// certo: se um objeto VELHO passa a apontar para um NOVO, a coleta do pedaco
/// novo nao enxerga esse ponteiro, porque ela nao percorre os velhos. Sem
/// cuidado, ela apaga um objeto que esta vivo. O cuidado se chama CONJUNTO
/// LEMBRADO, e a chave abaixo permite desliga-lo para medir o estrago.
/// </summary>
public sealed class Geracional : IColetor
{
    public Geracional(int fronteira, bool comConjuntoLembrado = true)
    {
        Fronteira = fronteira;
        ComConjuntoLembrado = comConjuntoLembrado;
    }

    /// <summary>
    /// A idade a partir da qual o objeto e considerado velho. Objetos nascidos
    /// antes desta coleta nao sao examinados.
    /// </summary>
    public int Fronteira { get; }

    /// <summary>
    /// Se o coletor usa o conjunto lembrado. Desligar e o jeito de medir para
    /// que ele serve.
    /// </summary>
    public bool ComConjuntoLembrado { get; }

    public string Nome => ComConjuntoLembrado ? "geracional" : "geracional sem lembranca";

    /// <summary>
    /// Quantos ponteiros de VELHO para NOVO existem, que e o tamanho do
    /// conjunto lembrado.
    ///
    /// E a medida que diz se a ideia inteira funciona: se esse numero fosse
    /// grande, o coletor novo teria que percorrer quase todos os velhos e a
    /// economia sumiria.
    /// </summary>
    public int Lembrados(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);

        var quantos = 0;
        foreach (var objeto in monte.Objetos.Values)
        {
            if (objeto.Nascimento >= Fronteira) continue;
            foreach (var apontado in objeto.Apontados)
                if (monte.Existe(apontado) && monte[apontado].Nascimento >= Fronteira) quantos++;
        }
        return quantos;
    }

    public Custo Coletar(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        var custo = new Custo();

        var novos = monte.Objetos.Values.Where(o => o.Nascimento >= Fronteira).Select(o => o.Id).ToHashSet();

        var vivos = new HashSet<int>();
        var fila = new Queue<int>();

        foreach (var raiz in monte.Raizes)
            if (monte.Existe(raiz) && novos.Contains(raiz) && vivos.Add(raiz)) fila.Enqueue(raiz);

        // O CONJUNTO LEMBRADO: os objetos novos apontados por algum velho
        // contam como raizes da coleta jovem. Sem isso, esses objetos parecem
        // inalcancaveis e sao apagados, com o velho segurando um ponteiro para
        // o nada.
        if (ComConjuntoLembrado)
        {
            foreach (var objeto in monte.Objetos.Values)
            {
                if (objeto.Nascimento >= Fronteira) continue;
                custo.Visitados++;
                foreach (var apontado in objeto.Apontados)
                    if (novos.Contains(apontado) && vivos.Add(apontado)) fila.Enqueue(apontado);
            }
        }

        while (fila.Count != 0)
        {
            var atual = fila.Dequeue();
            custo.Visitados++;
            foreach (var apontado in monte[atual].Apontados)
                if (novos.Contains(apontado) && vivos.Add(apontado)) fila.Enqueue(apontado);
        }

        custo.Marcados = vivos.Count;

        foreach (var id in novos)
        {
            if (vivos.Contains(id)) continue;
            monte.Apagar(id);
            custo.Apagados++;
        }

        return custo;
    }

    /// <summary>
    /// A versao HONESTA do conjunto lembrado: percorrer todos os velhos a cada
    /// coleta, que e o que esta implementado acima.
    ///
    /// Num sistema de verdade ninguem faz isso: uma BARREIRA DE ESCRITA anota o
    /// objeto velho no momento em que ele recebe um ponteiro para um novo, e a
    /// coleta le so a anotacao. O custo some do coletor e aparece em toda
    /// atribuicao de ponteiro do programa, o tempo todo, e essa troca e o preco
    /// escondido do coletor geracional.
    /// </summary>
    public int CustoDaBarreira(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        return monte.Objetos.Values.Where(o => o.Nascimento < Fronteira).Sum(o => o.Campos.Length);
    }
}
