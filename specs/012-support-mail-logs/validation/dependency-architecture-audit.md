# Dependency and architecture audit

PASS: cero paquetes nuevos. Entities no depende de capas; Business no usa Data/filesystem/procesos; Presentation no usa Data/filesystem/procesos; Data implementa launcher y log store. DI registra store, launcher, workflow y ViewModel singleton. SMTP, OAuth y envío silencioso ausentes.

