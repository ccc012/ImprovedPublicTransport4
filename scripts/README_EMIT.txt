ONE COMMAND — write all remaining language packs to Translations/*.txt:

  cd C:\Users\Lucas\source\repos\cs1_ipt4\scripts
  python emit_all_nine.py

This emits:
  no, sv, fi, hu  — from full native packs
  ro, vi          — from full native packs
  ms              — from id.txt with Malay vocabulary + overrides
  da              — already complete on disk (reported only)

Greek (el): add lang_packs_el.py EL dict then re-run, or write Translations/el.txt directly.

After emit, verify:
  python -c "from pathlib import Path
ROOT=Path('../Translations')
def p(f):
 k={}
 for line in f.read_text(encoding='utf-8').splitlines():
  if not line.strip(): continue
  i=line.find(' ')
  if i>0: k[line[:i]]=line[i+1:]
 return k
en=p(ROOT/'en.txt')
for lang in 'da fi no sv hu ro el vi ms'.split():
 tr=p(ROOT/f'{lang}.txt')
 non=sum(1 for k,v in en.items() if not k.startswith('CHANGELOG_') and tr.get(k)==v)
 tot=sum(1 for k,v in en.items() if tr.get(k)==v)
 print(lang, 'non-CL same', non, 'total same', tot)"
