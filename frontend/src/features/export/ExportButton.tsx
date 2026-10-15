/**
 * Descarga del historial completo hasta el mes actual en CSV (RF-38). Es un enlace común: el
 * navegador envía la cookie de sesión y guarda el archivo con el nombre que indica el backend.
 */
export function ExportButton() {
  return (
    <a className="export-link" href="/api/export.csv" download>
      Exportar historial (CSV)
    </a>
  )
}
