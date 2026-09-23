# Conexión Firebird

Esta carpeta contiene la infraestructura reutilizable de conexión de SysTools:

- `FirebirdConnectionFactory` construye una instancia nueva y cerrada de
  `FbConnection` por llamada mediante `FbConnectionStringBuilder`.
- `FirebirdConnectionProbe` adquiere la conexión, ejecuta únicamente `OpenAsync`
  y la dispone antes de entregar un estado controlado.
- `FirebirdErrorClassifier` traduce exclusivamente códigos numéricos conocidos;
  nunca usa mensajes localizados para inventar una causa.

La fábrica usa `ipEmpresa`, `rutaEmpresa`, `usuario` y `password` de la
configuración recibida, con puerto 3050, charset `ISO8859_1`, dialecto 3, timeout
de 5 segundos y pooling habilitado. No retiene configuración, conexiones ni
cadenas como estado.

El consumidor de cada conexión obtiene propiedad exclusiva y es responsable de
cerrarla o disponerla. No existe una conexión global. Esta capacidad no ejecuta
SQL, no consulta productos o fecha, no valida licencias y no agrega UI.

Los logs se limitan a operación, outcome, nombre del tipo de excepción y códigos
numéricos. Está prohibido registrar configuración, host, ruta, usuario, password,
licencia, cadena de conexión, mensaje crudo u objeto excepción.
