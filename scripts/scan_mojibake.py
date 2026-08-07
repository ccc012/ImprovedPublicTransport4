"""Report only REAL mojibake, not legitimate accented capitals.

'Ã' (U+00C3) is a valid character in text like 'Öffnet' / 'Économie' / 'À droite',
so counting raw C3 83 bytes produces false positives. Actual double-encoding always
shows up as 'Ã' immediately followed by a U+0080-U+00BF character (the re-encoded
continuation byte), which never happens in real prose.
"""
import os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else r'C:\Users\Lucas\source\repos\cs1_ipt4\Translations'
pattern = re.compile('[\u00c2-\u00c3\u00c5][\u0080-\u00bf]')

bad = False
for f in sorted(os.listdir(root)):
    if not f.endswith('.txt'):
        continue
    txt = open(os.path.join(root, f), encoding='utf-8-sig').read()
    hits = pattern.findall(txt)
    if hits:
        bad = True
        sample = ''.join(f'U+{ord(c):04X} ' for c in hits[0])
        print(f'{f:14} mojibake pairs = {len(hits):5}   first = {sample}')

print('NENHUM MOJIBAKE ENCONTRADO' if not bad else 'AINDA HA MOJIBAKE')