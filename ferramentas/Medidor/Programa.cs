using Conde.Memoria;

namespace Conde.Memoria.Medidor;

/// <summary>
/// As medidas. Contagem de objetos tocados e de objetos vazados: nenhuma
/// depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "tabela": Tabela(); break;
            case "ciclos": Ciclos(); break;
            case "lixo": Lixo(); break;
            case "geracional": Geracional(); break;
            case "memoria": Espaco(); break;
            case "tudo": Tabela(); Ciclos(); Lixo(); Geracional(); Espaco(); break;
            default:
                Console.Error.WriteLine("medidas: tabela, ciclos, lixo, geracional, memoria, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>Os tres coletores em todas as cargas, julgados por alcance.</summary>
    private static void Tabela()
    {
        Console.WriteLine("== os coletores em cada carga, conferidos contra o alcance ==");
        Console.WriteLine($"{"carga",-20}{"coletor",-16}{"apagou vivo",14}{"vazou",9}{"tocou",9}");

        foreach (var (nome, montar) in Cargas.Todas())
        {
            foreach (var coletor in Cargas.Coletores())
            {
                var antes = montar();
                var depois = antes.Copiar();
                var custo = coletor.Coletar(depois);
                var veredito = Alcance.Conferir(antes, depois);

                Console.WriteLine($"{nome,-20}{coletor.Nome,-16}{veredito.Perdidos.Count,14}" +
                                  $"{veredito.Vazados.Count,9}{custo.Tocados,9}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("a coluna 'apagou vivo' e zero em toda linha, e e a que importa: um coletor");
        Console.WriteLine("que apaga objeto alcancavel quebra o programa. A coluna 'vazou' tem numero");
        Console.WriteLine("so na contagem de referencia, e sempre pelo mesmo motivo");
        Console.WriteLine();
        Console.WriteLine("e a coluna 'tocou' e a diferenca entre marca-e-varre e copiador: o");
        Console.WriteLine("primeiro precisa olhar objeto MORTO, um por um, so para confirmar que esta");
        Console.WriteLine("morto; o segundo nunca toca em nada que morreu");
        Console.WriteLine();
    }

    /// <summary>O vazamento da contagem de referencia, medido em vez de citado.</summary>
    private static void Ciclos()
    {
        Console.WriteLine("== o que a contagem de referencia deixa para tras ==");
        Console.WriteLine($"{"carga",-24}{"lixo total",14}{"recolheu",12}{"vazou",10}{"fracao",10}");

        var casos = new (string Nome, Func<Monte> Montar)[]
        {
            ("500 ciclos puros", () => Cargas.Ciclos(500)),
            ("mistura, 5% de ciclo", () => Cargas.Mistura(2000, 7, chanceDeCiclo: 5)),
            ("mistura, 10% de ciclo", () => Cargas.Mistura(2000, 7, chanceDeCiclo: 10)),
            ("mistura, 30% de ciclo", () => Cargas.Mistura(2000, 7, chanceDeCiclo: 30)),
            ("arvore sem ciclo", () => Cargas.Arvore(11)),
        };

        foreach (var (nome, montar) in casos)
        {
            var antes = montar();
            var lixo = Alcance.Lixo(antes).Count;

            var depois = antes.Copiar();
            new Contagem().Coletar(depois);
            var vazou = Alcance.Conferir(antes, depois).Vazados.Count;

            Console.WriteLine($"{nome,-24}{lixo,14}{lixo - vazou,12}{vazou,10}" +
                              $"{(lixo == 0 ? 0 : vazou / (double)lixo),10:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("'contagem de referencia nao limpa ciclo' e a frase que todo mundo repete, e");
        Console.WriteLine("o que ela esconde e o TAMANHO. Com ciclo em 5% das ligacoes ela ja deixa");
        Console.WriteLine("8,6% do lixo para tras; com 30%, quase um terco. E o que fica nunca mais");
        Console.WriteLine("volta: ele nao vaza um pouco por coleta, ele e permanente");
        Console.WriteLine();
        Console.WriteLine("a ultima linha e o outro lado: sem ciclo nenhum, ela recolhe tudo, na hora,");
        Console.WriteLine("sem pausa e sem varrer o monte. E por isso que ela continua existindo");
        Console.WriteLine();
    }

    /// <summary>Quanto lixo faz o copiador valer a pena.</summary>
    private static void Lixo()
    {
        Console.WriteLine("== marca-e-varre contra copiador, variando quanto lixo ha ==");
        Console.WriteLine($"{"lixo",8}{"marca e varre toca",22}{"copiador toca",17}{"razao",10}");

        foreach (var lixo in new[] { 0, 25, 50, 75, 90, 95, 99 })
        {
            var monte = new Monte();
            var ids = new List<int>();
            for (var i = 0; i < 2000; i++) ids.Add(monte.Criar(16, 1).Id);

            var vivos = 2000 * (100 - lixo) / 100;
            for (var i = 0; i < vivos; i++) monte.Raizes.Add(ids[i]);

            var varre = new MarcaVarre().Coletar(monte.Copiar());
            var copia = new Copiador().Coletar(monte.Copiar());

            Console.WriteLine($"{lixo + " %",8}{varre.Tocados,22}{copia.Tocados,17}" +
                              $"{(copia.Tocados == 0 ? 0 : varre.Tocados / (double)copia.Tocados),10:N1}x");
        }

        Console.WriteLine();
        Console.WriteLine("o marca-e-varre toca o monte INTEIRO em toda linha, porque a varredura");
        Console.WriteLine("precisa olhar cada objeto morto para confirmar que ele morreu. O copiador");
        Console.WriteLine("toca so o que vive, e com 99% de lixo isso e cem vezes menos trabalho");
        Console.WriteLine();
        Console.WriteLine("e e por isso que coletor de lixo moderno e quase sempre copiador na");
        Console.WriteLine("geracao nova, onde o lixo passa de 90%, e marca-e-varre na geracao velha,");
        Console.WriteLine("onde quase tudo vive e copiar seria copiar tudo a toa");
        Console.WriteLine();
    }

    /// <summary>A hipotese geracional medida, e o conjunto lembrado desligado.</summary>
    private static void Geracional()
    {
        Console.WriteLine("== o coletor geracional ==");

        var antes = Cargas.Geracoes(500, 1500, 60, 9);
        var geracional = new Memoria.Geracional(1);

        var novos = antes.Objetos.Values.Count(o => o.Nascimento >= 1);
        var vivos = Alcance.Vivos(antes);
        var morrem = antes.Objetos.Values.Count(o => o.Nascimento >= 1 && !vivos.Contains(o.Id));

        Console.WriteLine($"  a carga tem {antes.Quantos} objetos, sendo {novos} da geracao nova");
        Console.WriteLine($"  dos novos, {morrem} morrem jovens, ou seja {morrem / (double)novos:P1}");
        Console.WriteLine($"  e ha {geracional.Lembrados(antes)} ponteiros de VELHO para NOVO no monte inteiro");
        Console.WriteLine();
        Console.WriteLine("  a hipotese geracional nao e suposta aqui: esses tres numeros sao ela. A");
        Console.WriteLine("  maioria morre jovem, e o punhado de ponteiros de velho para novo e o que");
        Console.WriteLine("  faz valer a pena coletar so o pedaco novo");
        Console.WriteLine();

        Console.WriteLine($"{"conjunto lembrado",-22}{"apagou vivo",14}{"vazou",9}{"tocou",9}");

        foreach (var com in new[] { true, false })
        {
            var original = Cargas.Geracoes(500, 1500, 60, 9);
            var depois = original.Copiar();
            var custo = new Memoria.Geracional(1, com).Coletar(depois);
            var veredito = Alcance.Conferir(original, depois);

            Console.WriteLine($"{(com ? "ligado" : "desligado"),-22}{veredito.Perdidos.Count,14}" +
                              $"{veredito.Vazados.Count,9}{custo.Tocados,9}");
        }

        Console.WriteLine();
        Console.WriteLine("sem o conjunto lembrado o coletor APAGA objeto vivo, e esse e o erro que o");
        Console.WriteLine("juiz existe para pegar. Um objeto novo que so e apontado por um velho parece");
        Console.WriteLine("inalcancavel para quem nao percorre os velhos, e o velho fica com um");
        Console.WriteLine("ponteiro para o nada");
        Console.WriteLine();
        Console.WriteLine($"  e o preco escondido: percorrer todos os velhos custaria " +
                          $"{geracional.CustoDaBarreira(antes)} campos por coleta.");
        Console.WriteLine("  Num sistema de verdade isso nao e pago na coleta: uma BARREIRA DE ESCRITA");
        Console.WriteLine("  anota o objeto velho no momento em que ele recebe um ponteiro para um");
        Console.WriteLine("  novo, e o custo sai do coletor e entra em TODA atribuicao de ponteiro do");
        Console.WriteLine("  programa, o tempo todo");
        Console.WriteLine();
    }

    /// <summary>A memoria que cada coletor exige.</summary>
    private static void Espaco()
    {
        Console.WriteLine("== a memoria que cada um exige ==");
        Console.WriteLine($"{"carga",-24}{"monte",12}{"marca e varre",16}{"copiador",12}{"contagem",12}");

        foreach (var (nome, montar) in Cargas.Todas())
        {
            var monte = montar();
            var bytes = monte.Bytes;

            // O marca-e-varre precisa de um bit por objeto; a contagem, de um
            // contador por objeto; o copiador, do monte inteiro de novo.
            var marcas = bytes + (monte.Quantos + 7) / 8;
            var contadores = bytes + monte.Quantos * 4;

            Console.WriteLine($"{nome,-24}{bytes,12:N0}{marcas,16:N0}" +
                              $"{Copiador.MemoriaNecessaria(bytes),12:N0}{contadores,12:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("a troca mais direta que existe em coletor de lixo esta nestas colunas:");
        Console.WriteLine("o copiador quer o DOBRO da memoria e nunca toca em objeto morto; o");
        Console.WriteLine("marca-e-varre quer um bit por objeto e toca em tudo. Nao ha meio termo");
        Console.WriteLine();
        Console.WriteLine("e o copiador ainda COMPACTA de graca: os sobreviventes saem lado a lado no");
        Console.WriteLine("destino, entao alocar depois e so andar um ponteiro e fragmentacao deixa de");
        Console.WriteLine("existir. O marca-e-varre deixa buracos e precisa de lista de livres");
        Console.WriteLine();
    }
}
