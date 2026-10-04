namespace Conde.Memoria;

/// <summary>
/// Um OBJETO do monte: um identificador, um tamanho, e campos que apontam para
/// outros objetos.
///
/// Nao ha dado nenhum alem do tamanho. Um coletor de lixo nao liga para o que o
/// objeto guarda: ele liga para quem aponta para quem, e todo o assunto cabe
/// nesse grafo.
/// </summary>
public sealed class Objeto
{
    public Objeto(int id, int tamanho, int campos)
    {
        Id = id;
        Tamanho = tamanho;
        Campos = new int[campos];
        Array.Fill(Campos, -1);
    }

    public int Id { get; }
    public int Tamanho { get; }

    /// <summary>Para quem cada campo aponta. Menos um quer dizer nulo.</summary>
    public int[] Campos { get; }

    /// <summary>Em que coleta o objeto nasceu, para o coletor geracional.</summary>
    public int Nascimento { get; set; }

    public IEnumerable<int> Apontados => Campos.Where(c => c >= 0);

    public override string ToString() => $"#{Id} ({Tamanho}b) -> [{string.Join(",", Apontados)}]";
}

/// <summary>
/// O MONTE: os objetos vivos e as RAIZES.
///
/// As raizes sao o ponto de partida de tudo. Num sistema de verdade elas sao as
/// variaveis da pilha, os registradores e as variaveis globais; aqui sao uma
/// lista, e a simplificacao nao tira nada do assunto, porque a pergunta
/// continua sendo a mesma: o que da para alcancar partindo delas.
/// </summary>
public sealed class Monte
{
    private readonly Dictionary<int, Objeto> objetos = [];
    private int proximoId;

    /// <summary>As raizes: o que o programa consegue nomear diretamente.</summary>
    public List<int> Raizes { get; } = [];

    public IReadOnlyDictionary<int, Objeto> Objetos => objetos;

    public int Quantos => objetos.Count;

    public int Bytes => objetos.Values.Sum(o => o.Tamanho);

    public Objeto this[int id] => objetos[id];

    public bool Existe(int id) => objetos.ContainsKey(id);

    public Objeto Criar(int tamanho, int campos, int coleta = 0)
    {
        var objeto = new Objeto(proximoId++, tamanho, campos) { Nascimento = coleta };
        objetos[objeto.Id] = objeto;
        return objeto;
    }

    public void Apagar(int id) => objetos.Remove(id);

    /// <summary>Liga o campo de um objeto a outro, ou a nulo com -1.</summary>
    public void Apontar(int de, int campo, int para)
    {
        objetos[de].Campos[campo] = para;
    }

    /// <summary>
    /// Quem aponta para o objeto dado. Custa uma varredura do monte inteiro, e e
    /// so por isso que contagem de referencia precisa guardar o contador: olhar
    /// para tras nao e barato.
    /// </summary>
    public IEnumerable<int> QuemAponta(int id) =>
        objetos.Values.Where(o => o.Apontados.Contains(id)).Select(o => o.Id);

    public Monte Copiar()
    {
        var saida = new Monte { proximoId = proximoId };
        foreach (var (id, objeto) in objetos)
        {
            var copia = new Objeto(id, objeto.Tamanho, objeto.Campos.Length) { Nascimento = objeto.Nascimento };
            Array.Copy(objeto.Campos, copia.Campos, objeto.Campos.Length);
            saida.objetos[id] = copia;
        }
        saida.Raizes.AddRange(Raizes);
        return saida;
    }

    public override string ToString() => $"{Quantos} objetos, {Bytes} bytes, {Raizes.Count} raizes";
}
