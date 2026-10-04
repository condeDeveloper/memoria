namespace Conde.Memoria;

/// <summary>O que uma coleta custou.</summary>
public sealed class Custo
{
    public int Visitados { get; set; }
    public int Marcados { get; set; }
    public int Apagados { get; set; }
    public int Copiados { get; set; }

    /// <summary>Quantos objetos o coletor TOCOU, vivos ou mortos.</summary>
    public int Tocados => Visitados + Apagados;

    public override string ToString() =>
        $"visitou {Visitados}, marcou {Marcados}, apagou {Apagados}" +
        (Copiados > 0 ? $", copiou {Copiados}" : "");
}

public interface IColetor
{
    string Nome { get; }
    Custo Coletar(Monte monte);
}

/// <summary>
/// MARCA E VARRE, de John McCarthy, 1960: o primeiro coletor de lixo que
/// existiu, escrito para o Lisp.
///
/// Duas fases. A primeira anda a partir das raizes marcando tudo que alcanca; a
/// segunda percorre o monte inteiro e apaga o que nao foi marcado.
///
/// O custo da primeira fase e proporcional ao que esta VIVO, e o da segunda ao
/// TAMANHO DO MONTE. Essa assimetria e o assunto: num monte grande com pouca
/// coisa viva, a varredura domina, e ela precisa olhar objeto morto um por um
/// so para confirmar que esta morto.
/// </summary>
public sealed class MarcaVarre : IColetor
{
    public string Nome => "marca e varre";

    public Custo Coletar(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        var custo = new Custo();

        var marcados = new HashSet<int>();
        var pilha = new Stack<int>();

        foreach (var raiz in monte.Raizes)
            if (monte.Existe(raiz) && marcados.Add(raiz)) pilha.Push(raiz);

        while (pilha.Count != 0)
        {
            var atual = pilha.Pop();
            custo.Visitados++;
            foreach (var apontado in monte[atual].Apontados)
                if (monte.Existe(apontado) && marcados.Add(apontado)) pilha.Push(apontado);
        }

        custo.Marcados = marcados.Count;

        foreach (var id in monte.Objetos.Keys.ToList())
        {
            if (marcados.Contains(id)) continue;
            monte.Apagar(id);
            custo.Apagados++;
        }

        return custo;
    }
}

/// <summary>
/// COPIADOR, de Cheney, 1970: em vez de apagar o que morreu, ele COPIA o que
/// vive para outro lugar e abandona o resto inteiro.
///
/// A conta vira proporcional so ao que esta vivo: objeto morto nunca e tocado.
/// Num monte com 95% de lixo, isso e vinte vezes menos trabalho que varrer.
///
/// O preco e DOBRO DE MEMORIA. O monte e dividido em duas metades e so uma esta
/// em uso por vez, entao metade da memoria fica parada o tempo todo esperando a
/// proxima coleta. E a troca mais direta que existe em coletor de lixo: tempo
/// contra espaco, sem meio termo.
///
/// E ele COMPACTA de graca: os sobreviventes saem lado a lado no destino, entao
/// alocar depois e so andar um ponteiro, e fragmentacao deixa de existir.
/// </summary>
public sealed class Copiador : IColetor
{
    public string Nome => "copiador";

    public Custo Coletar(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        var custo = new Custo();

        var vivos = new HashSet<int>();
        var fila = new Queue<int>();

        foreach (var raiz in monte.Raizes)
            if (monte.Existe(raiz) && vivos.Add(raiz)) fila.Enqueue(raiz);

        while (fila.Count != 0)
        {
            var atual = fila.Dequeue();
            custo.Visitados++;
            custo.Copiados++;
            foreach (var apontado in monte[atual].Apontados)
                if (monte.Existe(apontado) && vivos.Add(apontado)) fila.Enqueue(apontado);
        }

        custo.Marcados = vivos.Count;

        // O abandono do semiespaco inteiro: aqui ele aparece como apagar os que
        // nao foram copiados, e o custo NAO conta esses, porque num coletor de
        // verdade ninguem olha para eles. O espaco simplesmente vira o novo
        // destino da proxima vez.
        foreach (var id in monte.Objetos.Keys.ToList())
            if (!vivos.Contains(id)) monte.Apagar(id);

        return custo;
    }

    /// <summary>
    /// Quanta memoria o coletor precisa ter reservada, que e o dobro do monte.
    /// </summary>
    public static int MemoriaNecessaria(int bytesDoMonte) => bytesDoMonte * 2;
}

/// <summary>
/// CONTAGEM DE REFERENCIA, de Collins, 1960: cada objeto guarda quantos apontam
/// para ele, e morre quando o contador chega a zero.
///
/// A vantagem e que a memoria volta NA HORA, sem pausa, e isso importa muito em
/// sistema que nao pode parar. A desvantagem tem nome e e famosa: CICLO. Dois
/// objetos que apontam um para o outro tem contador um cada, nenhum chega a
/// zero, e os dois ficam para sempre, mesmo sem ninguem mais conseguir chegar
/// neles.
///
/// Este repositorio nao cita essa frase: ele mede quanto vaza, em cada carga.
/// </summary>
public sealed class Contagem : IColetor
{
    public string Nome => "contagem";

    public Custo Coletar(Monte monte)
    {
        ArgumentNullException.ThrowIfNull(monte);
        var custo = new Custo();

        // A contagem de verdade e incremental, atualizada a cada atribuicao. A
        // versao daqui recalcula tudo e depois propaga os zeros, e o resultado
        // e o MESMO: o que sobra e exatamente o que a contagem incremental
        // deixaria. Recalcular e mais lento e nao muda nenhuma conclusao.
        var contador = new Dictionary<int, int>();
        foreach (var id in monte.Objetos.Keys) contador[id] = 0;
        foreach (var raiz in monte.Raizes) if (contador.ContainsKey(raiz)) contador[raiz]++;
        foreach (var objeto in monte.Objetos.Values)
            foreach (var apontado in objeto.Apontados)
                if (contador.ContainsKey(apontado)) contador[apontado]++;

        var zerados = new Queue<int>(contador.Where(p => p.Value == 0).Select(p => p.Key));

        while (zerados.Count != 0)
        {
            var id = zerados.Dequeue();
            if (!monte.Existe(id)) continue;

            custo.Visitados++;
            foreach (var apontado in monte[id].Apontados)
            {
                if (!contador.ContainsKey(apontado)) continue;
                contador[apontado]--;
                if (contador[apontado] == 0) zerados.Enqueue(apontado);
            }

            monte.Apagar(id);
            custo.Apagados++;
        }

        custo.Marcados = monte.Quantos;
        return custo;
    }
}
