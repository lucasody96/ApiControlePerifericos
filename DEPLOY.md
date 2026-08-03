# Deploy — Publicacao online

Guia para publicar a aplicacao de forma gratuita e com acesso publico.

## Arquitetura

```
Navegador  ->  Vercel (frontend React/estatico)
                   |
                   v  (HTTPS, VITE_API_URL)
              Google Cloud Run (API .NET em container)
                   |
                   v  (MySQL/TLS)
              TiDB Cloud Starter (banco MySQL-compativel)
```

| Camada    | Servico                 | Custo      |
|-----------|-------------------------|------------|
| Frontend  | Vercel                  | Gratis     |
| API       | Google Cloud Run        | Gratis*    |
| Banco     | TiDB Cloud Starter      | Gratis     |

\* Cloud Run pede cartao no cadastro do GCP, mas nao cobra dentro do free tier.
Cloud Run e TiDB escalam a zero quando ociosos e religam sozinhos no acesso
(auto-resume) — nenhum servico precisa ser religado manualmente.

---

## Passo 1 — Banco: TiDB Cloud Starter

1. Criar conta em https://tidbcloud.com (login com GitHub, **sem cartao**).
2. Criar um cluster **Starter** (free) — escolher uma regiao proxima.
3. Em **Connect**, copiar os dados de conexao (host, porta `4000`, usuario no formato `xxxxxxxx.root`, senha e database — normalmente `test`).
4. Montar a connection string no formato do .NET/Pomelo (usada na env `ConnectionStrings__DefaultConnection`):

   ```
   Server=gateway01.SUA_REGIAO.prod.aws.tidbcloud.com;Port=4000;Database=test;User=SEU_PREFIXO.root;Password=SUA_SENHA;SslMode=Required;
   ```

   > O TiDB Cloud exige TLS — o `SslMode=Required` cuida disso. Se a conexao falhar
   > por validacao de certificado, usar `SslMode=VerifyFull` apontando o CA do sistema.

O banco e MySQL-compativel; o `PersistenceExtensions` (Infrastructure) fixa a versao
do servidor em MySQL 8.0.x.
As migrations sao aplicadas automaticamente no primeiro boot da API
(`Database.MigrateAsync()`), entao o banco pode comecar vazio.

---

## Passo 2 — API: Google Cloud Run

Pre-requisito: instalar o [Google Cloud CLI (`gcloud`)](https://cloud.google.com/sdk/docs/install) e rodar `gcloud auth login`.

1. Criar/selecionar um projeto GCP e habilitar billing (free tier).
2. Na raiz do repositorio (onde esta o `Dockerfile`), fazer o deploy a partir do codigo-fonte — o Cloud Run usa o `Dockerfile`:

   ```bash
   gcloud run deploy api-controle-perifericos \
     --source . \
     --region southamerica-east1 \
     --allow-unauthenticated \
     --set-env-vars "ASPNETCORE_ENVIRONMENT=Production" \
     --set-env-vars "ConnectionStrings__DefaultConnection=Server=...;Port=...;Database=defaultdb;User=avnadmin;Password=...;SslMode=Required;" \
     --set-env-vars "JWT__SecretKey=COLE_A_CHAVE_GERADA" \
     --set-env-vars "JWT__ValidIssuer=https://SUA-API.run.app" \
     --set-env-vars "JWT__ValidAudience=https://SUA-API.run.app" \
     --set-env-vars "Seed__AdminUsers__0__UserName=lucas.ody" \
     --set-env-vars "Seed__AdminUsers__0__Email=lucas@exemplo.com" \
     --set-env-vars "Seed__AdminUsers__0__Password=SENHA_FORTE_DO_ADMIN"
   ```

   > Na primeira vez o `gcloud` pergunta se pode criar o repositorio no Artifact Registry e habilitar APIs — aceite.
   > A URL publica (`https://SUA-API.run.app`) sai no fim do deploy. Anote-a.

3. Depois de saber a URL final, **reaplique** `JWT__ValidIssuer` e `JWT__ValidAudience` com ela (e o CORS no Passo 4). Para atualizar so as envs, sem novo build:

   ```bash
   gcloud run services update api-controle-perifericos --region southamerica-east1 \
     --update-env-vars "JWT__ValidIssuer=https://SUA-API.run.app,JWT__ValidAudience=https://SUA-API.run.app"
   ```

---

## Passo 3 — Frontend: Vercel

> O frontend fica em **repositorio proprio** (React + Vite), separado desta API.

1. Criar conta em https://vercel.com (login com GitHub).
2. **Import Project** -> selecionar o repositorio do frontend.
3. **Root Directory**: deixar vazio (o codigo esta na raiz do repositorio).
4. Framework Preset: **Vite** (detectado automaticamente).
5. Em **Environment Variables**, adicionar:

   | Nome           | Valor                       |
   |----------------|-----------------------------|
   | `VITE_API_URL` | `https://SUA-API.run.app`   |

   > `VITE_API_URL` e lido em build-time. Se mudar a URL da API depois, e preciso **redeploy** no Vercel.
6. Deploy. A URL final fica algo como `https://controle-perifericos.vercel.app`.

---

## Passo 4 — Conectar as pontas (CORS)

Com a URL do Vercel em maos, liberar o CORS na API e reapontar o JWT, e atualizar as envs do Cloud Run:

```bash
gcloud run services update api-controle-perifericos --region southamerica-east1 \
  --update-env-vars "Cors__AllowedOrigins__0=https://controle-perifericos.vercel.app"
```

Pronto — acessar a URL do Vercel e logar com o admin do seed.

---

## Variaveis de ambiente da API (referencia)

| Variavel                          | Exemplo / Observacao                                   |
|-----------------------------------|--------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`          | `Production`                                           |
| `ConnectionStrings__DefaultConnection` | Connection string do TiDB (porta 4000, com `SslMode=Required`) |
| `JWT__SecretKey`                  | Chave aleatoria forte (ver abaixo) — **segredo**       |
| `JWT__ValidIssuer`                | URL publica da API no Cloud Run                        |
| `JWT__ValidAudience`              | URL publica da API (ou do front)                       |
| `Cors__AllowedOrigins__0`         | URL publica do front no Vercel                         |
| `Seed__AdminUsers__0__UserName`   | Usuario admin inicial                                  |
| `Seed__AdminUsers__0__Email`      | Email do admin                                         |
| `Seed__AdminUsers__0__Password`   | Senha forte — **segredo**                              |

> O ASP.NET converte `__` (duplo underscore) em `:` na configuracao. Nenhum segredo vai para o Git — tudo fica nas envs do Cloud Run.

### Gerar uma `JWT__SecretKey` forte

PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
```

Bash:

```bash
openssl rand -base64 48
```

---

## Notas e limitacoes

- **Cold start**: o Cloud Run escala a zero quando ocioso; o primeiro request apos um periodo acorda o container (alguns segundos).
- **Migrations no startup**: aplicadas automaticamente. Se uma migration falhar, o container nao sobe — checar os logs no Cloud Run.
- **Hardening futuro** (ja anotado): rate limiting no login e migracao do token JWT de `localStorage` para cookie httpOnly.
- **Log.txt**: o logger em arquivo escreve no filesystem efemero do container (some a cada deploy/restart). Para producao seria melhor logar no stdout (capturado pelo Cloud Logging) — melhoria futura.
