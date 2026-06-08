# Instalacao Linux

Este deploy publica a API .NET, compila o frontend Angular, configura um servico systemd e coloca o Nginx na frente da aplicacao. O banco SQLite fica persistido fora da pasta de codigo.

## Requisitos

- Ubuntu/Debian com `sudo`.
- Acesso a internet para baixar dependencias e pacotes NuGet/npm.
- Porta 80 liberada no firewall.

O instalador tenta instalar automaticamente:

- .NET SDK 10
- Node.js 22 e npm
- Nginx

## Instalar

Na maquina Linux, a partir da raiz do repositorio:

```bash
sudo bash deploy/install-linux.sh --server-name seu-dominio-ou-ip
```

Se as dependencias ja estiverem instaladas:

```bash
sudo bash deploy/install-linux.sh --server-name seu-dominio-ou-ip --skip-deps
```

Ao final, o script mostra a URL e a senha inicial do usuario `admin` quando o banco ainda e novo.

## Onde fica cada parte

- API publicada: `/opt/farmacontrol/api`
- Frontend publicado: `/var/www/farmacontrol`
- Banco SQLite: `/var/lib/farmacontrol/farmacontrol.db`
- Variaveis do servico: `/etc/farmacontrol/farmacontrol.env`
- Servico systemd: `farmacontrol.service`
- Site Nginx: `/etc/nginx/sites-available/farmacontrol.conf`

## Operacao

```bash
sudo systemctl status farmacontrol.service
sudo journalctl -u farmacontrol.service -f
sudo systemctl restart farmacontrol.service
sudo nginx -t
sudo systemctl reload nginx
```

## Atualizar uma instalacao

Entre na pasta do repositorio atualizado e rode novamente:

```bash
sudo bash deploy/install-linux.sh --server-name seu-dominio-ou-ip
```

O instalador preserva os segredos existentes em `/etc/farmacontrol/farmacontrol.env` e republica API/frontend.

## HTTPS

O script configura HTTP na porta 80. Para usar HTTPS, configure o certificado no Nginx e redirecione HTTP para HTTPS no proprio Nginx. A API ja aceita os headers `X-Forwarded-*` enviados pelo proxy.
