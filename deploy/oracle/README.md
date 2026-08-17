# Oracle Cloud VM deployment

1. Copy this project to `/opt/izone` on an Ubuntu 22.04+ or Oracle Linux 8+ VM.
2. Create `/opt/izone/.env` from `.env.example` and set unique secrets. Do not commit this file.
3. Generate a persistent OpenIddict certificate in `/opt/izone/secrets/openiddict.pfx`:

   `openssl req -x509 -newkey rsa:4096 -keyout /tmp/openiddict.key -out /tmp/openiddict.crt -days 3650 -nodes -subj "/CN=izone" && openssl pkcs12 -export -out /opt/izone/secrets/openiddict.pfx -inkey /tmp/openiddict.key -in /tmp/openiddict.crt -passout pass:"$OPENIDDICT_CERT_PASSWORD" && rm /tmp/openiddict.key /tmp/openiddict.crt`

4. Start database migration and website: `docker compose up -d --build`.
5. Install the Nginx config from `deploy/oracle/nginx.conf`, set the real domain in `server_name`, then enable TLS with Certbot.

Only expose ports 80 and 443 publicly. PostgreSQL and port 8080 remain private to the VM.
