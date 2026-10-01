\# Plan de MessageGuard (un paso a la vez)



Marca cada paso cuando esté terminado, probado y con su commit.



\- \[ ] 1. Entorno: docker-compose con Postgres, RabbitMQ y Mailpit

\- \[ ] 2. Solución en Visual Studio con proyectos Domain, Application, Infrastructure, Api, Tests

\- \[ ] 3. Dominio: entidad Message, estados, Classification y eventos, con tests

\- \[ ] 4. Caso de uso SubmitMessage con repositorio en memoria y tests

\- \[ ] 5. Postgres: repositorio real + tabla outbox (mensaje + outbox en UNA transacción)

\- \[ ] 6. Relay del outbox hacia RabbitMQ (FOR UPDATE SKIP LOCKED) y ver eventos en el panel

\- \[ ] 7. Clasificador falso por palabras clave + consumidor ClassifyMessage (idempotente)

\- \[ ] 8. Mailer con Mailpit (SMTP) para mensajes aprobados

\- \[ ] 9. Front en Next.js: formulario + lista de estados con polling

\- \[ ] 10. Adaptador de Jev reemplazando el clasificador falso (con docs/jev.md)

\- \[ ] 11. Robustez: reintentos, dead-letter queue, fallo de Jev -> revisión manual

\- \[ ] 12. Extra: probar envío real a Yopmail y/o cambiar RabbitMQ por Kafka con un nuevo adaptador



\## Notas

\- Garantía del outbox: "al menos una vez" -> por eso la idempotencia en consumidores.

\- Cada paso: plan corto -> confirmación -> código -> tests -> commit.

