# FarmaControl

Aplicacao FarmaControl com backend .NET, frontend Angular e banco SQLite.

## Desenvolvimento local

API:

```bash
dotnet run --project src/backend/FarmaControl.Api/FarmaControl.Api.csproj
```

Frontend:

```bash
cd src/frontend
npm install
npm start
```

O frontend usa `/api` e o proxy local do Angular encaminha para `http://localhost:5076`.

## Instalacao em Linux

Use o instalador em `deploy/install-linux.sh`:

```bash
sudo bash deploy/install-linux.sh --server-name seu-dominio-ou-ip
```

Detalhes e comandos de operacao estao em [deploy/README.md](deploy/README.md).
