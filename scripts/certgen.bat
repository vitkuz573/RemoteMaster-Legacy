openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -config openssl.cnf -nodes
openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in cert.pem -passout pass:password