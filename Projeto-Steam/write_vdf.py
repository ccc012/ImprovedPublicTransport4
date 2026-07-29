# -*- coding: utf-8 -*-
"""Gera o .vdf de upload do Workshop. O changenote precisa ficar numa UNICA linha:
o parser KeyValues da Valve nao aceita quebra de linha real dentro do valor (da
"key name too long" / "got } in key"). Quebras vao como \n literal."""
import io

VDF = r"C:\steamcmd\atualizar_ipt4.vdf"
NL = chr(92) + "n"  # \n literal para o VDF

parts = [
 u"Versao 4.3.5 (canal BETA)",
 u"",
 u"CORRECOES",
 u"- Corrigido erro na inicializacao (Failed to find ImprovedPublicTransportMod assembly) que aparecia antes de o mapa carregar e desligava a nomeacao automatica de linhas (AutoLineColor). A causa era uma classe auxiliar que ainda localizava o mod por uma classe que deixou de ser o IUserMod na migracao para o CSLModsCommon. Agora o mod se localiza pela propria DLL, entao refatoracoes futuras nao quebram isso em silencio.",
 u"- O painel do Train Display voltou a aparecer ao seguir um veiculo. Ele procurava o veiculo seguido por tentativa e erro, mas o jogo guarda essa informacao num campo privado (CameraController.m_targetInstance) que nunca era encontrado. A opcao 'somente na camera em primeira pessoa' tambem passou a vir desligada por padrao, porque o modo seguir comum nao conta como primeira pessoa - era isso que fazia o painel parecer quebrado de fabrica.",
 u"- A aba Precos de Passagens agora mostra a tarifa resultante de cada tipo de transporte. Antes mostrava a contagem de passageiros, que numa cidade pequena ou pausada ficava sempre em zero e nao dizia nada sobre o preco. A contagem de passageiros passou para a dica do rotulo, junto com a tarifa original.",
 u"",
 u"IDIOMAS",
 u"- Bengali, hindi, indonesio e urdu agora aparecem no seletor de idioma do mod. Eles ja estavam totalmente traduzidos, mas faltava o arquivo de idioma do framework, entao eram inalcancaveis. Dos 23 idiomas do seletor, 18 tem traducao completa do mod.",
 u"- Todos os 19 arquivos de traducao reconferidos: mesmo conjunto de chaves, sem divergencia.",
 u"",
 u"DESEMPENHO",
 u"- O limitador de passageiros nas paradas resolvia o objeto de configuracoes uma vez por cidadao, dentro de um laco que roda milhares de vezes por quadro. Agora resolve uma vez por quadro.",
 u"",
 u"OUTROS",
 u"- O mod agora se identifica como BETA no painel de Opcoes (antes aparecia como ALPHA).",
 u"- Removida a aba de precos duplicada do painel de Opcoes; a aba original do painel Economia do jogo continua sendo a unica.",
]
changenote = NL.join(parts)

content = (
 u'"workshopitem"\n'
 u'{\n'
 u'    "appid" "255710"\n'
 u'    "publishedfileid" "3773802930"\n'
 u'    "contentfolder" "C:\\Users\\Lucas\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Addons\\Mods\\ImprovedPublicTransport4"\n'
 u'    "previewfile" "C:\\Users\\Lucas\\AppData\\Local\\Colossal Order\\Cities_Skylines\\Addons\\Mods\\ImprovedPublicTransport4\\ipt4-workshop-cover-under-2mb.jpg"\n'
 u'    "visibility" "0"\n'
 u'    "changenote" "' + changenote + u'"\n'
 u'}\n'
)

io.open(VDF, "w", encoding="utf-8", newline="\r\n").write(content)
print("vdf escrito; linhas =", content.count("\n"))
print("changenote tem quebra real?", "SIM (ruim)" if "\n" in changenote else "nao (ok)")
