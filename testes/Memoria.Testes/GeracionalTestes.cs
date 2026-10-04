using Conde.Memoria;
using Xunit;

namespace Conde.Memoria.Testes;

public class GeracionalTestes
{
    private static Monte Carga() => Cargas.Geracoes(500, 1500, 60, 9);

    /// <summary>
    /// A HIPOTESE GERACIONAL, medida em vez de suposta: a maioria dos objetos
    /// novos morre jovem.
    /// </summary>
    [Fact]
    public void AMaioriaDosNovosMorreJovem()
    {
        var monte = Carga();
        var vivos = Alcance.Vivos(monte);

        var novos = monte.Objetos.Values.Count(o => o.Nascimento >= 1);
        var morrem = monte.Objetos.Values.Count(o => o.Nascimento >= 1 && !vivos.Contains(o.Id));

        Assert.True(morrem / (double)novos > 0.9, $"morreram {morrem} de {novos}");
    }

    /// <summary>
    /// E o outro lado da hipotese: ha POUCOS ponteiros de velho para novo. Se
    /// fossem muitos, o coletor jovem teria que percorrer quase todos os velhos
    /// e a economia sumiria.
    /// </summary>
    [Fact]
    public void HaPoucosPonteirosDeVelhoParaNovo()
    {
        var monte = Carga();
        var lembrados = new Geracional(1).Lembrados(monte);

        Assert.True(lembrados > 0, "o cenario precisa ter pelo menos um");
        Assert.True(lembrados < monte.Quantos / 10, $"eram {lembrados} em {monte.Quantos} objetos");
    }

    [Fact]
    public void ComConjuntoLembradoNaoApagaNinguemVivo()
    {
        var antes = Carga();
        var depois = antes.Copiar();
        new Geracional(1).Coletar(depois);

        var veredito = Alcance.Conferir(antes, depois);
        Assert.True(veredito.Correto, veredito.ToString());
    }

    /// <summary>
    /// SEM o conjunto lembrado, ele apaga objeto vivo. O par de testes e a razao
    /// de o conjunto lembrado existir, e o juiz e quem pega.
    /// </summary>
    [Fact]
    public void SemConjuntoLembradoApagaObjetoVivo()
    {
        var antes = Carga();
        var depois = antes.Copiar();
        new Geracional(1, comConjuntoLembrado: false).Coletar(depois);

        var veredito = Alcance.Conferir(antes, depois);
        Assert.False(veredito.Correto);
        Assert.Equal(new Geracional(1).Lembrados(antes), veredito.Perdidos.Count);
    }

    /// <summary>
    /// Os que ele apaga sao exatamente os novos apontados so por velhos. O
    /// numero nao e aproximado: ele bate com a contagem do conjunto lembrado.
    /// </summary>
    [Fact]
    public void OsApagadosSaoExatamenteOsLembrados()
    {
        var antes = Carga();
        var depois = antes.Copiar();
        new Geracional(1, comConjuntoLembrado: false).Coletar(depois);

        var perdidos = Alcance.Conferir(antes, depois).Perdidos.ToHashSet();

        foreach (var id in perdidos)
        {
            Assert.True(antes[id].Nascimento >= 1, "so objeto novo pode ter sido apagado");
            Assert.All(antes.QuemAponta(id), quem => Assert.True(antes[quem].Nascimento < 1));
        }
    }

    /// <summary>
    /// O coletor jovem NAO apaga objeto velho, nem quando ele e lixo. Isso nao e
    /// defeito: e o que faz a coleta ser barata, e o lixo velho espera a coleta
    /// completa.
    /// </summary>
    [Fact]
    public void OColetorJovemNaoMexeNoVelho()
    {
        var monte = Carga();
        var velhosAntes = monte.Objetos.Values.Count(o => o.Nascimento < 1);

        new Geracional(1).Coletar(monte);
        var velhosDepois = monte.Objetos.Values.Count(o => o.Nascimento < 1);

        Assert.Equal(velhosAntes, velhosDepois);
    }

    [Fact]
    public void OColetorJovemRecolheOLixoJovem()
    {
        var antes = Carga();
        var lixoJovem = Alcance.Lixo(antes).Count(id => antes[id].Nascimento >= 1);

        var depois = antes.Copiar();
        var custo = new Geracional(1).Coletar(depois);

        Assert.Equal(lixoJovem, custo.Apagados);
    }

    /// <summary>
    /// A fronteira muda tudo: com ela em zero, todo objeto e novo e o coletor
    /// geracional vira um marca-e-varre comum.
    /// </summary>
    [Fact]
    public void ComFronteiraZeroEleViraUmColetorComum()
    {
        var antes = Carga();

        var geracional = antes.Copiar();
        new Geracional(0).Coletar(geracional);

        var comum = antes.Copiar();
        new MarcaVarre().Coletar(comum);

        Assert.Equal(comum.Quantos, geracional.Quantos);
    }

    [Fact]
    public void OCustoDaBarreiraEhOsCamposDosVelhos()
    {
        var monte = Carga();
        var esperado = monte.Objetos.Values.Where(o => o.Nascimento < 1).Sum(o => o.Campos.Length);
        Assert.Equal(esperado, new Geracional(1).CustoDaBarreira(monte));
    }

    [Fact]
    public void ONomeDizSeOConjuntoEstaLigado()
    {
        Assert.Equal("geracional", new Geracional(1).Nome);
        Assert.Equal("geracional sem lembranca", new Geracional(1, false).Nome);
    }

    [Fact]
    public void OSorteioEhReproduzivel()
    {
        var a = Cargas.Mistura(300, 42);
        var b = Cargas.Mistura(300, 42);
        Assert.Equal(a.Quantos, b.Quantos);
        Assert.Equal(a.Bytes, b.Bytes);
        Assert.Equal(Alcance.Vivos(a).Count, Alcance.Vivos(b).Count);
    }
}
