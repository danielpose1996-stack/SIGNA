"""Arranque X11 en CI con SQLite y HOME ficticios; no es una prueba manual de escritorio."""
import os
from pathlib import Path
import signal
import sqlite3
import subprocess
import sys
import time


def main():
    executable = Path(sys.argv[1]).resolve(strict=True)
    profile = Path(sys.argv[2]).resolve()
    profile.mkdir(parents=True, exist_ok=False)
    environment = os.environ | {
        'GESTOR_DOCENTE_MODE': 'demo',
        'GESTOR_DOCENTE_DEMO_DIR': str(profile),
        'GESTOR_DOCENTE_DATA_DIR': str(profile),
        'XDG_DATA_HOME': str(profile),
    }
    log = profile / 'startup.log'
    with log.open('w', encoding='utf-8') as output:
        process = subprocess.Popen([str(executable)], env=environment, stdout=output,
                                   stderr=subprocess.STDOUT, start_new_session=True)
        try:
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    raise RuntimeError(f'SIGNA terminó al iniciar: {process.returncode}')
                windows = subprocess.run(['xwininfo', '-root', '-tree'], check=True,
                                         capture_output=True, text=True, timeout=5).stdout
                if 'SIGNA — Sistema Integral de Gestión de Notas y Asistencias' in windows:
                    time.sleep(2)
                    if process.poll() is not None:
                        raise RuntimeError('SIGNA terminó después de abrir la ventana')
                    break
                time.sleep(.25)
            else:
                raise RuntimeError('No apareció la ventana principal de SIGNA en X11')
        except Exception:
            output.flush()
            print(log.read_text(encoding='utf-8'), file=sys.stderr)
            raise
        finally:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=5)
    database = profile / 'gestor-docente-demo.db'
    # Leer la copia ficticia sin crear otra si el arranque no llegó a SQLite.
    with sqlite3.connect(database.as_uri() + '?mode=ro', uri=True) as connection:
        if connection.execute('PRAGMA quick_check').fetchone()[0] != 'ok':
            raise RuntimeError('La base SQLite ficticia no superó la verificación')
        connection.execute('SELECT schema_version, payload FROM demo_workspace').fetchall()
    (profile / 'respaldo-ficticio.aulabk').write_bytes(b'sentinel de conservacion; no es un respaldo real\n')
    print(f'Ventana principal X11 y SQLite correctos: {executable}')


if __name__ == '__main__':
    main()
