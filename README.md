# memoria

Coletores de lixo do zero em C# e .NET 8: marca-e-varre, copiador de Cheney,
contagem de referência e geracional. O juiz é o **alcance** a partir das raízes,
que é a definição de lixo e não tem nada de aproximado.

```
$ dotnet medidor.dll tabela

carga               coletor         apagou vivo    vazou    tocou
500 ciclos          marca e varre             0        0     1000
500 ciclos          copiador                  0        0        0
500 ciclos          contagem                  0     1000        0

mistura de 2.000    marca e varre             0        0     2000
mistura de 2.000    copiador                  0        0      160
mistura de 2.000    contagem                  0      246     3188
```

## As duas perguntas que o juiz faz

Um objeto é lixo quando o programa não tem mais como chegar nele, por nenhum
caminho de ponteiros. Não importa se alguém ainda aponta para ele, não importa se
o contador dele é alto, não importa se ele foi usado agorinha.

A conferência tem **dois lados** que precisam valer juntos:

1. nada alcançável foi apagado — se falhar, o programa quebra;
2. nada inalcançável sobrou — se falhar, a memória vaza.

Um coletor que nunca apaga nada passa no primeiro e falha no segundo; um que
apaga tudo faz o contrário. Só os dois juntos dizem alguma coisa.

E a conferência precisa do monte **antes** e **depois**. Depois da coleta, um
objeto apagado por engano simplesmente não existe, e o monte resultante parece
perfeitamente consistente.

Há ainda uma terceira conferência que a contagem de sobreviventes deixa passar:
as **ligações**. Um coletor que copia objetos precisa consertar todo ponteiro que
apontava para o lugar antigo, e esquecer um deles produz um monte em que os
objetos certos existem e as ligações estão trocadas.

## O ciclo, medido em vez de citado

"Contagem de referência não limpa ciclo" é a frase que todo mundo repete. O que
ela esconde é o tamanho:

```
$ dotnet medidor.dll ciclos

carga                       lixo total    recolheu     vazou    fracao
500 ciclos puros                  1000           0      1000   100.0 %
mistura, 5% de ciclo              1853        1693       160     8.6 %
mistura, 10% de ciclo             1840        1594       246    13.4 %
mistura, 30% de ciclo             1795        1289       506    28.2 %
arvore sem ciclo                     0           0         0     0.0 %
```

Com ciclo em 5% das ligações ela já deixa 8,6% do lixo para trás; com 30%, quase
um terço. E o que fica nunca mais volta: não é um pouco por coleta, é permanente.

A última linha é o outro lado: sem ciclo nenhum, ela recolhe tudo, na hora, sem
pausa e sem varrer o monte. É por isso que ela continua existindo.

## Quanto lixo faz o copiador valer a pena

```
$ dotnet medidor.dll lixo

    lixo    marca e varre toca    copiador toca     razao
     0 %                  2000             2000       1.0x
    50 %                  2000             1000       2.0x
    90 %                  2000              200      10.0x
    99 %                  2000               20      100.0x
```

O marca-e-varre toca o monte **inteiro** em toda linha, porque a varredura precisa
olhar cada objeto morto para confirmar que ele morreu. O copiador toca só o que
vive.

É por isso que coletor de lixo moderno é quase sempre copiador na geração nova,
onde o lixo passa de 90%, e marca-e-varre na geração velha, onde quase tudo vive
e copiar seria copiar tudo à toa.

E a troca é direta: o copiador quer o **dobro** da memória, porque o monte é
dividido em duas metades e só uma está em uso por vez. Em compensação ele
**compacta de graça**: os sobreviventes saem lado a lado no destino, então alocar
depois é só andar um ponteiro, e fragmentação deixa de existir.

## A hipótese geracional, medida

```
$ dotnet medidor.dll geracional

  a carga tem 2000 objetos, sendo 1500 da geracao nova
  dos novos, 1444 morrem jovens, ou seja 96.3 %
  e ha 56 ponteiros de VELHO para NOVO no monte inteiro

conjunto lembrado        apagou vivo    vazou    tocou
ligado                             0        0     2000
desligado                         56        0     1500
```

A hipótese geracional não é suposta aqui: esses três números **são** ela. A maioria
morre jovem, e o punhado de ponteiros de velho para novo é o que faz valer a pena
coletar só o pedaço novo.

E a segunda tabela é o que acontece sem o **conjunto lembrado**: o coletor apaga 56
objetos vivos. Um objeto novo que só é apontado por um velho parece inalcançável
para quem não percorre os velhos, e o velho fica com um ponteiro para o nada. O
juiz pega, e o número de apagados bate exatamente com o número de ponteiros de
velho para novo.

O preço escondido disso é a **barreira de escrita**. Num sistema de verdade ninguém
percorre os velhos a cada coleta: uma barreira anota o objeto velho no momento em
que ele recebe um ponteiro para um novo. O custo sai do coletor e entra em **toda
atribuição de ponteiro** do programa, o tempo todo.

## Por que o monte é só um grafo

Não há dado nenhum nos objetos além do tamanho. Um coletor de lixo não liga para
o que o objeto guarda: ele liga para quem aponta para quem, e todo o assunto cabe
nesse grafo.

A busca de alcance é em **largura** e não em profundidade, e isso não é
preferência: um monte com uma lista encadeada de duzentos mil objetos estoura a
pilha numa busca recursiva, e lista encadeada longa é exatamente o que um monte
de verdade tem. Um teste roda com duzentos mil.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Monte.cs` | os objetos, os campos e as raízes |
| `Alcance.cs` | o juiz, e as três conferências |
| `Coletores.cs` | marca-e-varre, copiador e contagem de referência |
| `Geracional.cs` | o conjunto lembrado como chave que liga e desliga |
| `Cargas.cs` | lista, árvore, ciclos, mistura e gerações |

## Como rodar

```
dotnet test testes/Memoria.Testes/Memoria.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `tabela`, `ciclos`, `lixo`, `geracional`, `memoria` e `tudo`.

## Licença

MIT.
