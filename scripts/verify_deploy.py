import os

mod = os.path.join(os.environ['LOCALAPPDATA'], 'Colossal Order', 'Cities_Skylines',
                   'Addons', 'Mods', 'ImprovedPublicTransport4', 'Translations')

# 1) encoding fix reached the deployed files
for f, term in [('de.txt', b'DEPOT_STATS_FERRIES_IN_USE'),
                ('sv.txt', b'MOD_DESCRIPTION'),
                ('pt-br.txt', b'MOD_DESCRIPTION')]:
    data = open(os.path.join(mod, f), 'rb').read()
    i = data.find(term)
    chunk = data[i:i + 60]
    mojibake = b'\xc3\x83' in chunk          # double-encoded marker
    print(f'{f:12} mojibake={mojibake}   {chunk[:45]!r}')

print()
# 2) new toggle keys present everywhere
missing = []
for f in os.listdir(mod):
    if not f.endswith('.txt'):
        continue
    txt = open(os.path.join(mod, f), encoding='utf-8-sig').read()
    if 'SETTINGS_STOPSANDSTATIONS_ENABLE ' not in txt:
        missing.append(f)
print('packs sem SETTINGS_STOPSANDSTATIONS_ENABLE:', missing or 'nenhum')

# 3) orphan keys gone
orphan = [f for f in os.listdir(mod) if f.endswith('.txt')
          and 'COMMUTER_DESTINATION_' in open(os.path.join(mod, f), encoding='utf-8-sig').read()]
print('packs ainda com COMMUTER_DESTINATION_*:', orphan or 'nenhum')