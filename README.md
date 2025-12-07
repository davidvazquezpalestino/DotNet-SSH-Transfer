# Descargador de Copias de Seguridad por SSH

Aplicación de consola en .NET para descargar archivos de respaldo de forma segura desde un servidor remoto mediante SSH/SCP.

## Requisitos Previos

1. .NET 6.0 o superior
2. Herramientas de PuTTY (plink.exe y pscp.exe) disponibles en el PATH o especificar sus ubicaciones en la configuración
3. Acceso SSH al servidor remoto

## Instalación de Plink y PSCP

1. **Descarga las herramientas** desde la página oficial de PuTTY:
   - [Página de descarga oficial](https://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html)
   - Descarga el instalador de 64 bits: "64-bit x86 Installer"
   
2. **Opciones de instalación**:
   - **Instalador completo (recomendado)**: Instala todas las herramientas de PuTTY en `C:\Program Files\PuTTY\`
   - **Solo archivos necesarios**:
     - [plink.exe](https://the.earth.li/~sgtatham/putty/latest/w64/plink.exe)
     - [pscp.exe](https://the.earth.li/~sgtatham/putty/latest/w64/pscp.exe)

3. **Configuración del PATH**:
   - **Opción 1 (Recomendada)**: Agrega la ruta de instalación al PATH del sistema
     - Ejemplo: `C:\Program Files\PuTTY\`
   - **Opción 2**: Especifica la ruta completa en la configuración:
     ```json
     {
       "Plink": "C:\\ruta\\a\\plink.exe",
       "Pscp": "C:\\ruta\\a\\pscp.exe"
     }
     ```

4. **Verificación**:
   - Abre una terminal y ejecuta:
     ```
     plink -V
     pscp -V
     ```
   - Deberías ver la versión instalada de cada herramienta

## Configuración

1. Copiar `appsettings.template.json` a `appsettings.json`
2. Actualizar los siguientes valores en `appsettings.json`:
   - `Host`: Dirección del servidor SSH (IP o nombre de host)
   - `Port`: Puerto SSH (predeterminado: 22)
   - `Username`: Nombre de usuario SSH
   - `Password`: Contraseña SSH (puede dejarse vacía para solicitar al ejecutar)
   - `RemotePath`: Ruta remota donde se almacenan los archivos de respaldo
   - `LocalPath`: Directorio local donde se descargarán los archivos (predeterminado: "Backups" en el directorio de la aplicación)
   - `HostKey`: Huella digital de la clave del host SSH para verificación (opcional pero recomendado)
   - `Plink`: Ruta al ejecutable de plink (predeterminado: "plink.exe")
   - `Pscp`: Ruta al ejecutable de pscp (predeterminado: "pscp.exe")

## Métodos de Configuración

Puedes configurar la aplicación usando cualquiera de estos métodos (en orden de prioridad):

1. Argumentos por línea de comandos: `--clave=valor` (ejemplo: `--host=ejemplo.com --username=admin`)
2. Variables de entorno:
   - `DOWNLOADS_SSH_HOST`
   - `DOWNLOADS_SSH_PORT`
   - `DOWNLOADS_SSH_USERNAME`
   - `DOWNLOADS_SSH_PASSWORD`
   - `DOWNLOADS_SSH_REMOTEPATH`
   - `DOWNLOADS_SSH_HOSTKEY`
   - `DOWNLOADS_LOCAL_PATH`
   - `DOWNLOADS_PLINK_PATH`
   - `DOWNLOADS_PSCP_PATH`
3. Archivo `appsettings.json`
4. Valores predeterminados (definidos en el código)

## Uso

```bash
dotnet run -- --host=ejemplo.com --username=admin --remotePath=/ruta/a/respaldos
```

Si no se proporciona la contraseña en la configuración o variables de entorno, se te pedirá que la ingreses de forma segura.

## Notas de Seguridad

- Nunca subas el archivo `appsettings.json` con credenciales reales al control de versiones
- Considera usar autenticación por claves SSH en lugar de contraseñas cuando sea posible
- El `HostKey` ayuda a prevenir ataques de intermediario verificando la identidad del servidor
- La aplicación solicitará la contraseña si no se proporciona, lo cual es más seguro que almacenarla en la configuración

## Registro de Actividades

Por defecto, los registros se escriben tanto en la consola como en un archivo en el directorio `Logs`. Puedes configurar el registro en el archivo `appsettings.json` en la sección `Serilog`.
