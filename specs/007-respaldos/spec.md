# 007 — Respaldo y restauración

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, secciones III y IV; AGENTS.md, seguridad.

## Problema y objetivo

Conservar una copia recuperable y cifrada del trabajo académico, con destino local y Google Drive opcional, y permitir restaurarla en otro equipo.

## Historias y flujo

- Como docente, quiero generar un respaldo manual fechado sin sobrescribir los anteriores.
- Como docente, quiero restaurar un respaldo verificado conservando la posibilidad de recuperación ante un fallo.
- Como docente, quiero enviar manualmente una copia cifrada a una carpeta de Drive elegida por mí.

Flujo local propuesto: elegir destino → obtener copia SQLite consistente → cifrar y validar → guardar con nombre único → informar resultado. Restauración: elegir archivo → descifrar/verificar integridad y compatibilidad → revisar el reemplazo → restaurar mediante el mecanismo aprobado. Drive se construye después de validar el respaldo local.

## Reglas y validaciones

Del paquete: manual, local y Drive opcional; archivos fechados y cifrados; no sobrescribir; restauración en otro equipo. No copiar en caliente la base SQLite abierta: usar una API consistente de backup. Drive usa OAuth de mínimo privilegio y carpeta elegida. Las claves, su recuperación y el formato aún no están definidos.

## Criterios de aceptación propuestos

- **RES-01:** Dada una base válida, cuando se crea un respaldo local, entonces el archivo cifrado contiene una copia consistente y puede restaurarse con el mecanismo de claves aprobado.
- **RES-02:** Dados varios respaldos, cuando se genera otro incluso con la misma marca de tiempo, entonces ninguno de los existentes se sobrescribe y el nuevo recibe una identificación única.
- **RES-03:** Dado un archivo corrupto, una clave incorrecta o una versión incompatible, cuando se intenta restaurar, entonces se informa el motivo sin reemplazar los datos actuales.
- **RES-04:** Dado un respaldo válido y un segundo equipo compatible, cuando se restaura con el acceso a claves aprobado, entonces se recuperan los datos y relaciones respaldados.
- **RES-05:** Dado un fallo durante la restauración, cuando termina la operación, entonces los datos previos siguen recuperables conforme al procedimiento aprobado y no se informa éxito.
- **RES-06:** Dado el uso local sin conexión, cuando se crea/restaura un respaldo, entonces no requiere autenticación en Drive.
- **RES-07:** Dada una cuenta autorizada y carpeta seleccionada, cuando se ordena manualmente subir un respaldo, entonces solo se envía el archivo ya cifrado y se confirma el resultado de la subida.
- **RES-08:** Dado un rechazo OAuth o fallo de red, cuando se intenta subir, entonces se informa el fallo, el respaldo local permanece disponible y no se comunica una subida exitosa.

## Estados de UI

Sin datos: destino o archivo por elegir. Cargando: pasos de respaldo/verificación/restauración/subida. Éxito: ruta, fecha y resultado comprobado. Error: explicación accionable, sin revelar claves. Restauración requiere mostrar qué se reemplazará antes de realizarlo; flujo por aprobar.

## Modelo y migraciones

Formato propuesto con versión y metadatos mínimos de compatibilidad/integridad. Mecanismos criptográficos y autenticación de metadatos pendientes de decisión técnica. Las migraciones se validarán sobre una copia antes del reemplazo; la política de compatibilidad debe quedar documentada.

## Seguridad y errores

No incluir tokens, claves ni contraseñas en Git ni en logs. Cifrar antes de subir. Custodia de claves y credenciales mediante mecanismo aprobado para cada sistema operativo. Retención y consentimiento institucional pendientes; no seleccionar una cuenta o carpeta automáticamente.

## Pruebas previstas

Backup con base abierta mediante API consistente; restauración completa; copia corrupta/truncada; clave incorrecta; versión incompatible; colisión de nombre; fallo de escritura/restauración; uso offline; subida fallida con adaptador simulado. Verificación en ambos sistemas antes de declarar portabilidad.

## Decisiones abiertas

D-09/D-10: cifrado, claves y recuperación, formato, compatibilidad, retención y cuentas/carpeta autorizadas. Definir el procedimiento de recuperación ante fallo, el almacenamiento de OAuth y los permisos concretos antes de integrar Drive.
