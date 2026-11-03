# Pruebas de integración y distribuidas

SIPAU tiene tres niveles de prueba, separados a propósito:

| Nivel | Proyecto y filtro | Qué ejecuta | Qué necesita |
|---|---|---|---|
| Unitarias | `UPZMG.Tests` | Lógica pura: árbol de decisión, validación cruzada, regresión, reglas de captura e importación, caché del modelo con datos falsos. EF en memoria donde hace falta un contexto. | Nada |
| Integración | `UPZMG.IntegrationTests`, `Category=Integration` | La API real (controladores, autenticación JWT, permisos, repositorios) dentro del proceso de prueba, contra una base SQL Server **propia de la corrida**. | Un SQL Server |
| Distribuidas | `UPZMG.IntegrationTests`, `Category=Distributed` | La API y la Web como **dos procesos del sistema operativo**, cada uno en su puerto, comunicándose sólo por HTTP, como en producción. | Un SQL Server y la API y la Web compiladas |

Las pruebas de integración y las distribuidas crean una base `upzmg_it_<fecha>_<n>`, le aplican las migraciones y las mismas semillas que la base de desarrollo (`test-data.sql`, `seed-official-catalogs.sql`, `seed-permissions.sql`, `seed-academic-history.sql`) y la borran al terminar. No tocan la base del equipo.

## Cómo correrlas

Con el contenedor `upzmg_db` levantado, desde la raíz del repositorio.

PowerShell:

```powershell
$env:UPZMG_IT_SQL = "Server=localhost,1433;User ID=sa;Password=$(docker exec upzmg_db printenv MSSQL_SA_PASSWORD);TrustServerCertificate=True;Encrypt=False"

dotnet test UPZMG.Tests
dotnet test UPZMG.IntegrationTests --filter Category=Integration

# Las distribuidas arrancan UPZMG.API.dll y UPZMG.Web.dll desde su carpeta bin; compílalas antes.
dotnet build UPZMG.sln
dotnet test UPZMG.IntegrationTests --filter Category=Distributed
```

Git Bash:

```bash
export UPZMG_IT_SQL="Server=localhost,1433;User ID=sa;Password=$(docker exec upzmg_db printenv MSSQL_SA_PASSWORD);TrustServerCertificate=True;Encrypt=False"
dotnet test UPZMG.IntegrationTests
```

Sin `UPZMG_IT_SQL`, las pruebas que necesitan base aparecen como **omitidas** con el motivo, no como fallidas.

Variables opcionales:

- `UPZMG_IT_API_DIR` y `UPZMG_IT_WEB_DIR`: carpetas de una publicación (`dotnet publish`) de la API y de la Web, para no usar las de `bin`.
- `UPZMG_IT_KEEP_DB=1`: conserva la base al terminar, para revisar lo que dejaron las pruebas.

La Web de las pruebas distribuidas sirve HTTPS con el certificado de desarrollo de .NET (`dotnet dev-certs https --trust` si la máquina no lo tiene).

## Qué demuestra cada prueba

| Prueba | Evidencia |
|---|---|
| `AccessControlTests.EachAreaReachesWhatItsRoleGrantsAndNothingElse` | Matriz de acceso por área: cada cuenta sembrada contra diez endpoints, 200 o 403 según el permiso de su rol. |
| `AccessControlTests.AnAreaCannotWriteIntoAnotherAreasRecords` | Recursos Humanos no puede registrar ni importar alumnos; Control Escolar no puede registrar empleados. Nada queda guardado. |
| `AccessControlTests.AWithdrawnPermissionAppliesOnTheVeryNextRequest` | Un permiso retirado aplica en la siguiente petición, con el mismo token: la API consulta la base en cada petición. |
| `AccessControlTests.ASuspendedAccountLosesAccessAndCannotGetANewToken` | Una cuenta suspendida pierde el acceso y la API ya no le emite tokens; su 403 es lo que permite a la Web distinguirla de un fallo. |
| `AccessControlTests.APermissionAloneOpensWhatItsDescriptionPromises` | Un rol con un solo permiso abre lo que ese permiso describe, sin depender de otro, y nada más. |
| `AccessControlTests.OnlyAProcessThatKnowsTheSharedSecretCanSignSomebodyIn` | Sólo un proceso con el secreto compartido puede autenticar a alguien en la API. Responde 403, no el 401 de una contraseña equivocada. |
| `ConcurrencyTests.TwelveSimultaneousCapturesOfOneStudentStoreItOnce` | Doce capturas simultáneas de la misma matrícula: una se guarda y once reciben 409. Ninguna termina en 500. |
| `ConcurrencyTests.TwoAdministratorsSuspendingEachOtherLeaveOneStanding` | Dos administradores que se suspenden mutuamente a la vez: sólo una baja procede y siempre queda uno activo. |
| `ConcurrencyTests.TwelveRequestsForOneSlotCreateOneGroup` | Doce peticiones por el mismo espacio y hora: se crea un grupo y once reciben 409. |
| `ConcurrencyTests.TwelveRequestsForOneNameCreateOneRoleAndOneSpace` | Doce altas simultáneas del mismo nombre crean un rol y un espacio; las demás reciben 409. |
| `ConcurrencyTests.TwelveRequestsForOneCodeCreateOnePeriod` | Doce altas simultáneas del mismo código crean un periodo; las demás reciben 409, no un error de servidor. |
| `ConcurrencyTests.ColdRequestsForTheModelShareOneTraining` | Ocho peticiones simultáneas con el modelo sin entrenar comparten un solo entrenamiento. |
| `ConcurrencyTests.RetrainingsWhileOthersReadLeaveOneConsistentModel` | Reentrenamientos concurrentes con lectores: todo responde 200 y el resumen coincide con el listado. |
| `BulkImportTests.TheCheckWritesNothingAndGivesEveryRowItsReason` | La revisión de un CSV no escribe nada y explica cada fila rechazada: dato faltante, duplicado en el archivo, duplicado en la base, lengua desconocida, CURP corta. |
| `BulkImportTests.ConfirmedRowsAreStoredTogetherAndListedBack` | Las filas confirmadas se guardan juntas y se devuelven como confirmación. Repetir el archivo no duplica nada. |
| `BulkImportTests.AFailureHalfwayThroughTheWriteLeavesNothingBehind` | Si la base rechaza una fila a mitad de la escritura, se revierte el lote completo. |
| `ReportLifecycleTests.AnAnswerOutsideTheCatalogueIsNotAcceptedAsOfficial` | Un periodo o un nivel inventados no se aceptan como formato oficial (422); en borrador sí se pueden guardar. |
| `ReportLifecycleTests.ATableOverItsLimitIsNotAcceptedAsOfficial` | 61 líneas donde la plantilla permite 60 no se aceptan como formato oficial, aunque cuadren con su total. |
| `ReportLifecycleTests.EverySeededCaptureIsWithinItsLists` | Las ocho capturas sembradas cumplen esa regla: ningún borrador existente queda atorado por ella. |
| `ReportLifecycleTests.ASectionOfTheBoardReportIsNotOverwrittenWithoutReadingIt` | Quien guarda un apartado que otra persona cambió mientras lo redactaba recibe el texto de ella, en vez de borrarlo sin saberlo. |
| `ReportLifecycleTests.SendingAFormatAgainDoesNotAdvanceTheBoardReport` | Enviar otra vez un formato ya enviado no mueve el avance del Informe de Junta: cuenta formatos, no capturas. |
| `ReportLifecycleTests.ADraftChangesUntilItIsSubmittedAndThenNever` | Ciclo de un formato oficial: borrador editable, protección contra sobrescritura (412), envío incompleto rechazado (422), envío, bloqueo (409) y lectura sólo por su autor. |
| `WebApiProcessTests.ASessionOpenedInTheWebReadsTheDatabaseThroughTheApi` | Con la Web y la API en procesos separados, el inicio de sesión de la Web pasa por la API y la página muestra cifras calculadas por la API. |
| `WebApiProcessTests.WhenTheApiStopsTheWebSaysSoAndRecoversWhenItReturns` | Se detiene la API: la Web sigue arriba, conserva la sesión y dice que no pudo consultar. Al volver la API, la misma sesión se recupera sola. |
| `WebApiProcessTests.WithTheApiDownSigningInDoesNotBlameThePassword` | Con la API detenida, iniciar sesión dice que el sistema no está disponible, no que la contraseña está mal. |
| `WebApiProcessTests.ASuspendedAccountIsSignedOutAndToldWhy` | Una cuenta dada de baja con la sesión abierta sale al inicio de sesión con el motivo y pierde su cookie. |
| `WebApiProcessTests.ASignInLandsOnTheScreenThatAskedForItAndNeverOnAnotherSite` | Tras iniciar sesión se vuelve a la pantalla que lo pidió, nunca a una dirección de otro sitio. |
| `WebApiProcessTests.TheRiskScreenSpeaksPlainlyToTheAreasThatActOnIt` | Para un área, la pantalla de riesgo explica el porcentaje y no habla de pliegues, Gini ni intervalos de confianza. |
