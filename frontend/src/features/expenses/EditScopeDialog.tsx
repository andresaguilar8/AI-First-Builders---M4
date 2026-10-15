import type { EditScope } from '../../api/types'

interface EditScopeDialogProps {
  onChoose: (scope: EditScope) => void
  onCancel: () => void
}

/** Alcance de la edición de un recurrente: solo el mes visualizado o también los siguientes (RF-10). */
export function EditScopeDialog({ onChoose, onCancel }: EditScopeDialogProps) {
  return (
    <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="edit-scope-title">
      <h3 id="edit-scope-title">¿A qué meses aplicar el cambio?</h3>
      <p>Los meses anteriores no cambian.</p>
      <div className="dialog-actions">
        <button type="button" onClick={() => onChoose('ThisMonth')}>
          Solo este mes
        </button>
        <button type="button" onClick={() => onChoose('ThisAndFollowing')}>
          Este mes y los siguientes
        </button>
        <button type="button" onClick={onCancel}>
          Volver
        </button>
      </div>
    </div>
  )
}
