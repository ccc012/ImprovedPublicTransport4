import io

files = [
    'workshop-description-brazilian.txt',
    'workshop-description-czech.txt',
    'workshop-description-danish.txt',
    'workshop-description-indonesian.txt',
    'workshop-description-malay.txt',
    'workshop-description-norwegian.txt',
    'workshop-description-portuguese.txt',
    'workshop-description-thai.txt',
    'workshop-description-vietnamese.txt',
]

for fn in files:
    with io.open(fn, encoding='utf-8') as f:
        c = f.read()
    res = []
    open_q = True
    for ch in c:
        if ch == '"':
            res.append('\u201c' if open_q else '\u201d')
            open_q = not open_q
        else:
            res.append(ch)
    c2 = ''.join(res)
    if c2 != c:
        with io.open(fn, 'w', encoding='utf-8', newline='') as f:
            f.write(c2)
        print(fn, 'FIXED')
    else:
        print(fn, 'unchanged')
