import sys, re

def check(fn):
    with open(fn, encoding='utf-8') as f:
        c = f.read()
    n = len(c)
    q = c.count('"')
    issues = []
    if q:
        issues.append(f'quotes={q}')
    if n > 8000:
        issues.append(f'OVER8000={n}')
    if '[list]' in c or '[*]' in c:
        issues.append('has list tags')
    for tag in ['h1','h2','b','i','url','code','hr']:
        o = len(re.findall(r'\[' + tag + r'(=[^\]]*)?\]', c))
        cl = c.count(f'[/{tag}]')
        if o != cl:
            issues.append(f'{tag} unbalanced open={o} close={cl}')
    status = 'OK' if not issues else 'ISSUES: ' + '; '.join(issues)
    print(f'{fn:45s} chars={n:5d} {status}')

for fn in sys.argv[1:]:
    check(fn)
