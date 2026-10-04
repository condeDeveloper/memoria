using Conde.Memoria;
using Xunit;

namespace Conde.Memoria.Testes;

public class ColetorTestes
{
    /// <summary>
    /// A propriedade que nao pode falhar em nenhum coletor, em nenhuma carga:
    /// objeto alcancavel nao pode ser apagado. Se falhar, o programa quebra.
    /// </summary>
    [Fact]
    public void NenhumColetorApagaObjetoVivo()
    {
        foreach (var (nome, montar) in Cargas.Todas())
            foreach (var coletor in Cargas.Coletores())
            {
                var antes = montar();
                var depois = antes.Copiar();
                coletor.Coletar(depois);

                var veredito = Alcance.Conferir(antes, depois);
                Assert.True(veredito.Correto, $"{coletor.Nome} em {nome}: {veredito}");
            }
    }

    /// <summary>E as ligacoes continuam certas, nao so os objetos.</summary>
    [Fact]
    public void NenhumColetorBaguncaAsLigacoes()
    {
        foreach (var (nome, montar) in Cargas.Todas())
            foreach (var coletor in Cargas.Coletores())
            {
                var antes = montar();
                var depois = antes.Copiar();
                coletor.Coletar(depois);
                Assert.True(Alcance.LigacoesIntactas(antes, depois), $"{coletor.Nome} em {nome}");
            }
    }

    [Fact]
    public void MarcaEVarreEhCorretoECompleto()
    {
        foreach (var (nome, montar) in Cargas.Todas())
        {
            var antes = montar();
            var depois = antes.Copiar();
            new MarcaVarre().Coletar(depois);

            var veredito = Alcance.Conferir(antes, depois);
            Assert.True(veredito.Correto && veredito.Completo, $"{nome}: {veredito}");
        }
    }

    [Fact]
    public void OCopiadorEhCorretoECompleto()
    {
        foreach (var (nome, montar) in Cargas.Todas())
        {
            var antes = montar();
            var depois = antes.Copiar();
            new Copiador().Coletar(depois);

            var veredito = Alcance.Conferir(antes, depois);
            Assert.True(veredito.Correto && veredito.Completo, $"{nome}: {veredito}");
        }
    }

    /// <summary>
    /// A contagem de referencia e correta e NAO e completa: ela deixa o ciclo
    /// para tras. O par de testes e o assunto inteiro dela.
    /// </summary>
    [Fact]
    public void AContagemNaoLimpaCiclo()
    {
        var antes = Cargas.Ciclos(50);
        var depois = antes.Copiar();
        new Contagem().Coletar(depois);

        var veredito = Alcance.Conferir(antes, depois);
        Assert.True(veredito.Correto);
        Assert.Equal(100, veredito.Vazados.Count);
        Assert.Equal(100, depois.Quantos);
    }

    [Fact]
    public void AContagemLimpaTudoQuandoNaoHaCiclo()
    {
        var antes = Cargas.Arvore(8);
        antes.Raizes.Clear();

        var depois = antes.Copiar();
        new Contagem().Coletar(depois);

        Assert.Equal(0, depois.Quantos);
    }

    /// <summary>
    /// O quanto ela vaza depende da carga, e e esse numero que a frase repetida
    /// esconde.
    /// </summary>
    [Fact]
    public void OVazamentoDaContagemCresceComOsCiclos()
    {
        var pouco = Vazamento(5);
        var muito = Vazamento(30);

        Assert.True(pouco > 0);
        Assert.True(muito > pouco * 2);
    }

    private static double Vazamento(int chanceDeCiclo)
    {
        var antes = Cargas.Mistura(2000, 7, chanceDeCiclo: chanceDeCiclo);
        var lixo = Alcance.Lixo(antes).Count;

        var depois = antes.Copiar();
        new Contagem().Coletar(depois);
        return Alcance.Conferir(antes, depois).Vazados.Count / (double)lixo;
    }

    /// <summary>
    /// O copiador nunca toca em objeto morto, e o marca-e-varre toca em todos.
    /// Com muito lixo, isso e uma ordem de grandeza.
    /// </summary>
    [Fact]
    public void OCopiadorNaoTocaEmObjetoMorto()
    {
        var monte = new Monte();
        var ids = new List<int>();
        for (var i = 0; i < 1000; i++) ids.Add(monte.Criar(16, 1).Id);
        for (var i = 0; i < 50; i++) monte.Raizes.Add(ids[i]);

        var varre = new MarcaVarre().Coletar(monte.Copiar());
        var copia = new Copiador().Coletar(monte.Copiar());

        Assert.Equal(1000, varre.Tocados);
        Assert.Equal(50, copia.Tocados);
    }

    [Fact]
    public void ComMonteTodoVivoOsDoisEmpatam()
    {
        var monte = Cargas.Lista(500);
        var varre = new MarcaVarre().Coletar(monte.Copiar());
        var copia = new Copiador().Coletar(monte.Copiar());

        Assert.Equal(varre.Tocados, copia.Tocados);
    }

    [Fact]
    public void OCopiadorPedeODobroDaMemoria()
    {
        Assert.Equal(2000, Copiador.MemoriaNecessaria(1000));
    }

    [Fact]
    public void ColetarDuasVezesNaoMudaNada()
    {
        foreach (var coletor in Cargas.Coletores())
        {
            var monte = Cargas.Mistura(500, 11);
            coletor.Coletar(monte);
            var depoisDaPrimeira = monte.Quantos;

            coletor.Coletar(monte);
            Assert.Equal(depoisDaPrimeira, monte.Quantos);
        }
    }

    [Fact]
    public void ColetarMonteVazioNaoQuebra()
    {
        foreach (var coletor in Cargas.Coletores())
        {
            var monte = new Monte();
            var custo = coletor.Coletar(monte);
            Assert.Equal(0, custo.Apagados);
        }
    }
}
