for f in ['fr.txt','hu.txt','sv.txt','pt.txt','pt-br.txt']:
    content = open(f'C:/Users/Lucas/source/repos/cs1_ipt4/Translations/{f}', 'rb').read()
    for i, b in enumerate(content):
        if b > 127:
            ctx = content[max(0,i-10):i+10]
            print(f'{f}: offset {i} byte {b:02X} context: {" ".join(f"{x:02X}" for x in ctx)}')
            break