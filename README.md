## terminal chat src code:

See `features/llm` branch:
  https://github.com/microsoft/terminal/blob/938b3ec2f2f5e1ba37a951dfdee078b1a7a40394/src/cascadia/QueryExtension/ExtensionPalette.cpp#L26

## Win Terminal Chat Settings => Endpoint Examples

- put the URL into Win Terminal Chat settings Endpoint field
  - To avoid confusiong about the word `endpoint`:
    - `endpoint` means the terminal chat settings field (points to intermediate dotnet API or azure openai instance)
    - `backend` means where the dotnet API forwards the request to (i.e. ollama, groq.com, OpenAI, etc.)
- ollama
  `ollama serve`
  - FYI backend param (in query string, not for win term chat settings) defaults to ollama http://127.0.0.1:11434/v1
  - https://fake.openai.azure.com:5000/answer?model=codellama
  - https://fake.openai.azure.com:5000/answer?model=llama3
- groq.com
  - https://fake.openai.azure.com:5000/answer?model=llama3-70b-8192&backend=https://api.groq.com/openai/v1
  - provide actual API key
- OpenAI
  - https://fake.openai.azure.com:5000/answer?model=gpt-4o&backend=https://api.openai.com/v1
  - provide actual API key

## commands

```shell
# curls
curl -v -k -X POST "https://localhost:5000/fake" # -k/--insecure (no verify cert)
curl --ssl-revoke-best-effort https://fake.openai.azure.com:5000/fake # ignore missing CRL (revocation) but still verify cert => https://superuser.com/questions/1800816/

# mkcert
winget install FiloSottile.mkcert
mkcert -install
mkcert -cert-file cert.pem -key-file key.pem fake.openai.azure.com localhost 127.0.0.1 ::1 # I also added 192.168.1.X 

# /etc/hosts
notepad C:\Windows\System32\drivers\etc\hosts
# add:
# 127.0.0.1 fake.openai.azure.com

# mitmproxy
mitmproxy --insecure # ignore self-signed certs if using mitmproxy to snoop on traffic to/from dotnet api intermediate (BE VERY CAREFUL w/ this)
```
