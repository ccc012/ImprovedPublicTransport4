# -*- coding: utf-8 -*-
"""Gera o .vdf de upload do Workshop.

Sobre quebras de linha nos valores, ver o comentario longo em kv_value(): o parser
NAO processa escapes, entao \\n literal aparece como texto na pagina - foi o que
aconteceu com a nota da versao 4.3.5. Valores multi-linha usam quebra de linha
real; o que quebra o parse e aspa dupla reta nao escapada.

Deliberadamente NAO escreve:
  - "tags"  -> setar substitui a lista inteira; o item ja tem "Mod", que e a
               unica tag correta. Um nome invalido apagaria "Mod" em silencio.
  - "title" -> ausente, o Workshop mantem o titulo atual. Passar por engano
               sobrescreve o nome do item.
Ver workshop-metadata-en.md.
"""
import io
import os

VDF = r"C:\steamcmd\atualizar_ipt4.vdf"
DESC = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    "workshop-description-en.txt")
CONTENT = r"C:\Users\Lucas\AppData\Local\Colossal Order\Cities_Skylines\Addons\Mods\ImprovedPublicTransport4"

# Capa: se o arquivo nao existir, a chave "previewfile" e OMITIDA e o Workshop
# mantem a imagem que ja esta na pagina. Apontar para um arquivo inexistente faz o
# upload inteiro falhar com "Failed to update workshop item (File Not Found)".
#
# Nao guardar a capa dentro de CONTENT: aquela pasta e gerada pelo build (o target
# DeployToModDirectory), entao qualquer coisa colocada la a mao desaparece - foi
# exatamente assim que a capa se perdeu antes do envio da 4.3.8. Alem disso, o que
# esta em CONTENT vai no download do jogador, e uma capa de 2 MB nao serve pra nada
# no jogo. O lugar certo e junto deste script.
PREVIEW = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                       "ipt4-workshop-cover.jpg")

CHANGENOTE = [
 u"Version 4.3.8 - no longer BETA",
 u"",
 u"THE MAINTENANCE BUG IS FIXED AT ITS ROOT",
 u"- If your public transport was draining millions per week for no visible reason, that was this. The weekly maintenance calculation divided by a vehicle capacity that some custom vehicles report as zero. That produced an invalid number, which became a maintenance cost near the largest value the game can hold, and then every vehicle on the line was added on top of it.",
 u"- Invalid values now fall back to the game's own weekly cost, line totals are added up with room to spare, and large legitimate amounts are charged in bounded steps instead of one impossible transaction.",
 u"",
 u"IT ALSO REPAIRS SAVES THE BUG ALREADY DAMAGED",
 u"- When a city loads, the mod clears only the negative public-transport income and expense entries this overflow left behind, and corrects the totals that referenced them. Valid economy history and every service other than public transport are left alone. You do not need to start a new city.",
 u"",
 u"ALL 23 LANGUAGES ARE NOW COMPLETE",
 u"- Czech, Dutch, Slovak, Thai and Turkish appeared in the language list but had no translation file, so they quietly showed English. They are translated now, along with every changelog entry, so the panel no longer shows raw text like CHANGELOG_4_3_6_1 in most languages.",
 u"- The translation percentage under the language selector was reading a status file that had never been updated, and reported 76% for sixteen languages that were in fact finished. It now reports what is actually there.",
 u"",
 u"EARLIER IN THIS RELEASE LINE",
 u"- Fixed a startup error that appeared before the map loaded and switched off automatic line naming and colouring.",
 u"- The Train Display panel shows again when you follow a vehicle. It could not find the vehicle you were following, and the 'first-person camera only' option was on by default even though the ordinary follow mode does not count as first person - together that made the panel look permanently broken.",
 u"- The Ticket Prices tab now shows the resulting fare for each transport type instead of a passenger count that read zero in a small or paused city.",
 u"- Stop passenger limiting no longer re-reads your settings once per citizen inside a loop that runs thousands of times per frame.",
 u"",
 u"CREDITS",
 u"- The absorbed mods are other people's work and the description now links each one to its original item and names its author. If a feature here is useful to you, the person who wrote it deserves to hear so on their own page.",
 u"",
 u"Remember to unsubscribe the standalone versions of the absorbed mods - running both patches the same game code twice.",
]


def kv_value(source):
    """Prepara um valor multi-linha para o KeyValues do workshop vdf.

    Comprovado empiricamente na nota de alteracao da versao 4.3.5, que foi enviada
    com \\n literal e apareceu na pagina com o texto "\\n" no meio das frases:
    **o parser do vdf de workshop NAO processa escapes**. Consequencias:

      - \\n literal NAO vira quebra de linha, aparece como texto. Usar quebra
        de linha REAL dentro do valor entre aspas.
      - barra invertida e literal, entao caminhos como
        %LOCALAPPDATA%\\Colossal Order\\... vao como estao, sem dobrar.
      - aspa dupla e o que realmente quebra: sem escapes, ela encerra o valor e o
        parser passa a ler o resto do texto como nome de chave. Foi isso que deu
        "key name too long (1235 chars)" na primeira tentativa de upload - nao as
        quebras de linha, como pareceu na epoca.

    Por isso a fonte usa aspas tipograficas e este teste recusa aspas retas.
    """
    if isinstance(source, list):
        parts = source
    else:
        raw = io.open(source, encoding="utf-8").read()
        parts = raw.replace("\r\n", "\n").rstrip("\n").split("\n")

    joined = "\n".join(parts)
    assert '"' not in joined, (
        'aspas dupla reta no valor: encerraria o valor e quebraria o parse. '
        'Trocar por aspas tipograficas.')
    return joined


changenote = kv_value(CHANGENOTE)
# Validado mesmo sem ir pro vdf: garante que o arquivo continua colavel no
# editor do Workshop e sem aspas retas, que quebrariam um envio futuro.
description = kv_value(DESC)

tem_capa = os.path.exists(PREVIEW)

# A chave "description" NAO entra. Testado em 2026-07-29 contra o item 3773802930:
# com ela, o upload sobe o conteudo e falha em "Committing update...ERROR! Failed to
# update workshop item (Invalid Parameter)". Sem ela, o mesmo vdf commita "Success".
# Nao e tamanho - falhou com 10723 chars e falhou igual com 7968, e o teste de
# biseccao isolou a chave.
#
# Consequencia pratica: a descricao da pagina se atualiza a mao, colando
# workshop-description-en.txt no editor do Workshop. O arquivo continua sendo a fonte
# da verdade e fica versionado aqui; so a publicacao dele e manual.
content = (
 u'"workshopitem"\n'
 u'{\n'
 u'    "appid" "255710"\n'
 u'    "publishedfileid" "3773802930"\n'
 u'    "contentfolder" "' + CONTENT + u'"\n'
 + (u'    "previewfile" "' + PREVIEW + u'"\n' if tem_capa else u'') +
 u'    "visibility" "0"\n'
 u'    "changenote" "' + changenote + u'"\n'
 u'}\n'
)

io.open(VDF, "w", encoding="utf-8", newline="\r\n").write(content)
print("vdf escrito em", VDF)
print("  changenote e description com quebras de linha reais")
print("  changenote:  %d chars" % len(changenote))
print("  description: %d chars - NAO vai no vdf, colar a mao (ver comentario)" % len(description))
print("  tags: nao definido (mantem 'Mod')")
print("  title: nao definido (mantem o atual)")
if tem_capa:
    print("  previewfile:", PREVIEW)
else:
    print("  previewfile: OMITIDO - mantem a capa que ja esta na pagina.")
    print("               Pra trocar a capa, por o .jpg em %s" % PREVIEW)
