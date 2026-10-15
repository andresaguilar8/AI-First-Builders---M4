import { useState } from 'react'

interface OverduePaymentDialogProps {
  /** Fecha actual del backend (Buenos Aires): la fecha de pago no puede superarla (RF-30). */
  today: string
  /** Sin fecha = pagado hoy. */
  onConfirm: (paidOn?: string) => void
  onCancel: () => void
}

/** Pago de un gasto vencido: pregunta si se pagó hoy o permite ingresar la fecha real (RF-29). */
export function OverduePaymentDialog({ today, onConfirm, onCancel }: OverduePaymentDialogProps) {
  const [choosingDate, setChoosingDate] = useState(false)
  const [paidOn, setPaidOn] = useState('')
  const [error, setError] = useState<string | null>(null)

  const confirmDate = () => {
    if (!paidOn) return setError('Ingresá la fecha de pago.')
    // Las fechas `yyyy-MM-dd` se comparan como texto.
    if (paidOn > today) return setError('La fecha de pago no puede ser posterior a hoy.')
    onConfirm(paidOn)
  }

  return (
    <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="overdue-payment-title">
      <h3 id="overdue-payment-title">¿Lo pagaste hoy?</h3>
      {choosingDate ? (
        <>
          <label htmlFor="overdue-payment-date">Fecha de pago</label>
          <input
            id="overdue-payment-date"
            type="date"
            max={today}
            value={paidOn}
            onChange={(event) => setPaidOn(event.target.value)}
          />
          {error && <p className="form-error">{error}</p>}
          <div className="dialog-actions">
            <button type="button" onClick={confirmDate}>
              Confirmar
            </button>
            <button type="button" onClick={onCancel}>
              Cancelar
            </button>
          </div>
        </>
      ) : (
        <div className="dialog-actions">
          <button type="button" onClick={() => onConfirm()}>
            Sí, hoy
          </button>
          <button type="button" onClick={() => setChoosingDate(true)}>
            No, otra fecha
          </button>
          <button type="button" onClick={onCancel}>
            Cancelar
          </button>
        </div>
      )}
    </div>
  )
}
