# Improved Public Transport 4 (IPT4)

IPT4 é uma continuação prática do legado do IPT/IPT2/IPT3 para **Cities: Skylines**, focada em dar mais controle sobre o transporte público sem obrigar o jogador a lidar com dezenas de mods separados. A ideia é simples: facilitar o ajuste das linhas, reduzir comportamento ruim da frota, melhorar paradas e veículos, e deixar o sistema mais previsível para cidades pequenas e grandes.

## O que o IPT4 melhora

Para jogadores comuns, o IPT4 melhora principalmente estes pontos:

- controle mais claro da quantidade de veículos por linha
- possibilidade de escolher quais veículos podem operar em cada linha
- ferramentas para reduzir comboios de ônibus, bondes e outros veículos andando grudados
- mais informações úteis sobre paradas, linhas e veículos
- ajustes de preço de passagem, quando esse recurso estiver ativado
- melhorias de qualidade de vida vindas de integrações consolidadas no próprio projeto

## Requisitos

- **Cities: Skylines 1**
- versão do jogo compatível com a base atual do projeto
- dependências do ecossistema usadas pelo mod, quando exigidas pela instalação do jogador
- DLCs são opcionais, mas alguns recursos e tipos de transporte só aparecem se o conteúdo correspondente existir no jogo

## Instalação

### Instalação manual

1. Feche o jogo.
2. Baixe ou copie a versão publicada do IPT4.
3. Coloque a pasta do mod dentro de:
   - `C:\Users\<seu-usuario>\AppData\Local\Colossal Order\Cities_Skylines\Addons\Mods\`
4. Abra o jogo e ative o mod no gerenciador de conteúdo, se necessário.

### Atualizando uma instalação existente

1. Feche o jogo.
2. Substitua a pasta antiga do IPT4 pela nova.
3. Abra o jogo novamente.
4. Revise as opções do mod antes de continuar um save importante.

## Como usar as configurações simples

Se você quer apenas jogar com menos complicação, este é o caminho recomendado:

### 1. Ajuste o básico das linhas

Abra uma linha de transporte e use o painel do IPT4 para:

- aumentar ou reduzir a frota manualmente
- ver quantos veículos estão ativos ou em fila
- trocar a garagem quando isso estiver disponível

### 2. Ative o controle de orçamento, se quiser algo mais automático

O modo de orçamento deixa o jogo controlar a frota com base no sistema de orçamento da linha. É útil para quem não quer administrar linha por linha o tempo todo.

### 3. Use a distribuição de veículos para reduzir comboios

Se vários veículos da mesma linha ficam chegando juntos, aumente a configuração de distribuição de veículos. Em geral, isso já melhora bastante o espaçamento sem exigir mudanças profundas na cidade.

### 4. Verifique paradas problemáticas

Ao clicar numa parada, o IPT4 mostra informações que ajudam a identificar gargalos:

- passageiros esperando
- embarques e desembarques
- tempo até desistência
- navegação para parada anterior e próxima

### 5. Ajuste tipos de veículos apenas quando fizer sentido

Se uma linha estiver usando modelos inadequados, abra a seleção de veículos e limite a operação aos modelos que você realmente quer naquela linha.

## Configuração recomendada para começar

Para a maioria dos jogadores, vale começar assim:

- controle por orçamento: ativado, se você prefere menos microgerenciamento
- distribuição de veículos: ativada
- editor de veículos: deixar no padrão até entender o impacto
- preços de passagem: manter no padrão no primeiro teste
- limites de parada/estação: só mexer se houver superlotação ou comportamento estranho

## Recursos principais

### Controle de linhas

Permite administrar quantidade de veículos, fila de spawn, tipos permitidos e comportamento de espaçamento por linha.

### Informações de paradas

Ajuda a descobrir onde a rede está falhando, com foco em leitura rápida durante o jogo.

### Editor de veículos

Permite ajustar capacidade, custo e velocidade em casos específicos. Para a maioria dos jogadores, isso é opcional.

### Ajustes de passagem

Quando o recurso estiver ativo, você pode alterar o preço por tipo de transporte para mudar demanda e receita.

## Compatibilidade para jogadores

- O IPT4 busca consolidar funções que antes exigiam mods separados.
- Isso reduz conflito, mas não elimina incompatibilidades com outros mods que mexem exatamente nas mesmas telas, sistemas de linha ou patches Harmony.
- Se você já usa mods antigos com funções parecidas, o ideal é não manter duplicidade.

## Solução rápida de problemas

### O mod aparece, mas algo não mudou no jogo

- confirme se o mod está ativado
- feche e abra o jogo após atualizar a pasta
- teste em um save separado antes de concluir que a função não funciona

### Veículos continuam andando em comboio

- aumente a agressividade da distribuição de veículos
- confirme se a linha específica está com a opção ativa
- verifique se a cidade não tem gargalos extremos que forçam todos os veículos a parar no mesmo ponto

### A linha usa veículos errados

- abra a seleção de tipos de veículo
- remova os modelos que não deveriam operar naquela linha
- confira se a garagem da linha oferece veículos compatíveis

### O jogo apresentou conflito com outro mod

- desative mods que alterem as mesmas funções de transporte
- teste o IPT4 sozinho ou com uma combinação mínima
- reative os outros mods um a um, se precisar localizar o conflito

## Para usuários avançados

Esta seção é menor de propósito. O foco do projeto continua sendo o uso prático no jogo.

### Configurações detalhadas

Usuários avançados podem explorar:

- comportamento detalhado da distribuição de veículos
- limites de passageiros por tipo de parada e estação
- personalização de preços por modal
- editor de veículos para capacidade, custo e velocidade
- integrações de recursos específicos herdados de versões anteriores

### Compatibilidade

O principal cuidado é evitar sobreposição com outros mods que:

- alterem quantidade de veículos por linha
- mudem lógica de embarque/desembarque
- modifiquem precificação de transporte
- instalem patches Harmony nas mesmas rotinas do jogo

### Solução de problemas avançada

Se algo fugir do esperado:

- teste com menos mods ativos
- revise logs e mensagens de conflito
- compare o comportamento em um save novo e em um save antigo
- confirme se DLCs e dependências realmente existem no ambiente usado

## Créditos

O IPT4 existe graças ao trabalho acumulado da comunidade de modding de Cities: Skylines. Este projeto preserva o crédito histórico das versões anteriores e das integrações que deram origem à base atual.

Autores e mantenedores históricos citados pelo projeto incluem, entre outros:

- Dontcryjustdie
- BloodyPenguin
- Nyoko
- egi
- llunak
- Vectorial1024
- macsergey
- dymanoid
- TaradinoC

## Licença

Os créditos e as licenças dos componentes originais devem ser preservados. Licenças específicas de integrações e partes incorporadas permanecem nos respectivos diretórios do projeto, especialmente dentro de `Integration/` quando aplicável.

## Documentação técnica

Este README não é documentação para desenvolvedores. Se a parte técnica do IPT4 precisar crescer, a documentação de arquitetura, build, compatibilidade interna e decisões de projeto poderá ficar em uma área separada do repositório no futuro.