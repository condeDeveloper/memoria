using Conde.Memoria;
using Xunit;

namespace Conde.Memoria.Testes;

/// <summary>
/// O juiz testado antes de julgar. Se o alcance estiver errado, todo coletor
/// deste repositorio concorda com ele e tudo passa.
/// </summary>
public class AlcanceTestes
{
    [Fact]
    public void MonteVazioNaoTemVivoNemLixo()
    {
        var monte = new Monte();
        Assert.Empty(Alcance.Vivos(monte));
        Assert.Empty(Alcance.Lixo(monte));
    }

    [Fact]
    public void ObjetoSemRaizEhLixo()
    {
        var monte = new Monte();
        monte.Criar(8, 0);
        Assert.Empty(Alcance.Vivos(monte));
        Assert.Single(Alcance.Lixo(monte));
    }

    [Fact]
    public void ARaizEhViva()
    {
        var monte = new Monte();
        var objeto = monte.Criar(8, 0);
        monte.Raizes.Add(objeto.Id);
        Assert.Single(Alcance.Vivos(monte));
    }

    [Fact]
    public void OAlcanceAtravessaACadeiaInteira()
    {
        var monte = Cargas.Lista(500);
        Assert.Equal(500, Alcance.Vivos(monte).Count);
        Assert.Empty(Alcance.Lixo(monte));
    }

    /// <summary>
    /// A busca e em LARGURA e nao em profundidade, e nao e preferencia: com uma
    /// lista de cem mil objetos, a versao recursiva estoura a pilha, e lista
    /// encadeada longa e exatamente o que um monte de verdade tem.
    /// </summary>
    [Fact]
    public void AListaMuitoLongaNaoEstouraAPilha()
    {
        var monte = Cargas.Lista(200_000);
        Assert.Equal(200_000, Alcance.Vivos(monte).Count);
    }

    /// <summary>
    /// Um CICLO sem raiz e lixo, mesmo todo mundo dentro dele tendo quem aponte.
    /// E a definicao de lixo que separa alcance de contagem.
    /// </summary>
    [Fact]
    public void OCicloSemRaizEhLixo()
    {
        var monte = Cargas.Ciclos(10);
        Assert.Empty(Alcance.Vivos(monte));
        Assert.Equal(20, Alcance.Lixo(monte).Count);

        // E todo objeto do ciclo tem alguem apontando para ele.
        foreach (var id in monte.Objetos.Keys) Assert.NotEmpty(monte.QuemAponta(id));
    }

    /// <summary>E o mesmo ciclo, com uma raiz, esta todo vivo.</summary>
    [Fact]
    public void OCicloComRaizEstaTodoVivo()
    {
        var monte = Cargas.Ciclos(10);
        monte.Raizes.Add(0);
        Assert.Equal(2, Alcance.Vivos(monte).Count);
    }

    [Fact]
    public void RaizQueNaoExisteMaisNaoQuebra()
    {
        var monte = new Monte();
        monte.Raizes.Add(999);
        Assert.Empty(Alcance.Vivos(monte));
    }

    [Fact]
    public void PonteiroParaObjetoApagadoNaoQuebra()
    {
        var monte = new Monte();
        var a = monte.Criar(8, 1);
        var b = monte.Criar(8, 0);
        monte.Apontar(a.Id, 0, b.Id);
        monte.Raizes.Add(a.Id);
        monte.Apagar(b.Id);

        Assert.Single(Alcance.Vivos(monte));
    }

    /// <summary>
    /// O veredito precisa dos DOIS montes. Depois da coleta, um objeto apagado
    /// por engano simplesmente nao existe, e o monte resultante parece
    /// perfeitamente consistente.
    /// </summary>
    [Fact]
    public void OVereditoPegaOsDoisTiposDeErro()
    {
        var antes = Cargas.Mistura(200, 3);

        var apagouDemais = antes.Copiar();
        foreach (var id in antes.Objetos.Keys.ToList()) apagouDemais.Apagar(id);
        var primeiro = Alcance.Conferir(antes, apagouDemais);
        Assert.False(primeiro.Correto);
        Assert.True(primeiro.Completo);

        var naoApagouNada = antes.Copiar();
        var segundo = Alcance.Conferir(antes, naoApagouNada);
        Assert.True(segundo.Correto);
        Assert.False(segundo.Completo);
    }

    /// <summary>
    /// Conferir so a contagem de sobreviventes deixa passar um coletor que
    /// bagunca as LIGACOES. Este teste e o que pega isso.
    /// </summary>
    [Fact]
    public void AsLigacoesPrecisamContinuarCertas()
    {
        var antes = Cargas.Arvore(8);
        var depois = antes.Copiar();
        Assert.True(Alcance.LigacoesIntactas(antes, depois));

        var bagunçado = antes.Copiar();
        var raiz = antes.Raizes[0];
        bagunçado.Apontar(raiz, 0, bagunçado.Objetos.Keys.Max());
        Assert.False(Alcance.LigacoesIntactas(antes, bagunçado));
    }

    [Fact]
    public void ACopiaEhIndependenteDoOriginal()
    {
        var original = Cargas.Arvore(5);
        var copia = original.Copiar();

        copia.Apagar(copia.Objetos.Keys.First());
        Assert.NotEqual(original.Quantos, copia.Quantos);
    }
}
