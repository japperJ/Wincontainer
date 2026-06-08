path = r'C:\Users\jptrs\AppData\Local\Programs\Microsoft VS Code Insiders-new\77dfb21e21\resources\app\out\nls.messages.js'
text = open(path, 'r', encoding='utf-8', errors='ignore').read()
idx = text.find('Enables **Advanced Autopilot**')
print('IDX', idx)
print('SNIPPET', text[idx:idx+1000])
