import React, { useEffect, useRef } from 'react';
import ReactDOM from 'react-dom';

interface ModalProps {
  title?: string;
  onClose: () => void;
  children: React.ReactNode;
  footer?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
  closeDisabled?: boolean;
}

// Basic focus trap inside the modal content
export const Modal: React.FC<ModalProps> = ({ title, onClose, children, footer, size='lg', closeDisabled }) => {
  const elRef = useRef<HTMLDivElement | null>(null);
  if (!elRef.current) elRef.current = document.createElement('div');

  useEffect(() => {
    const el = elRef.current!;
    document.body.appendChild(el);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape' && !closeDisabled) onClose();
      if (e.key === 'Tab') {
        const focusables = el.querySelectorAll<HTMLElement>(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        if (focusables.length === 0) return;
        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
      }
    }
    window.addEventListener('keydown', onKey);
    return () => {
      window.removeEventListener('keydown', onKey);
      document.body.style.overflow = previousOverflow;
      document.body.removeChild(el);
    };
  }, [onClose, closeDisabled]);

  useEffect(() => {
    // Auto-focus first input
    const firstInput = elRef.current?.querySelector<HTMLElement>('input,select,textarea,button:not([data-modal-close])');
    if (firstInput) setTimeout(() => firstInput.focus(), 30);
  }, []);

  const dialog = (
    <>
      <div
        className="modal-backdrop fade show"
        style={{ backgroundColor: 'rgba(0,0,0,.5)', zIndex: 1049 }}
        onClick={() => { if (!closeDisabled) onClose(); }}
      />
      <div className="modal d-block" role="dialog" aria-modal="true" style={{ zIndex: 1050 }}>
        <div className={`modal-dialog modal-${size}`} role="document">
          <div className="modal-content">
          {(title || !closeDisabled) && (
            <div className="modal-header">
              {title && <h5 className="modal-title">{title}</h5>}
              {!closeDisabled && (
                <button type="button" className="btn-close" aria-label="Close" data-modal-close onClick={onClose} />
              )}
            </div>
          )}
          <div className="modal-body">
            {children}
          </div>
          {footer && (
            <div className="modal-footer">
              {footer}
            </div>
          )}
          </div>
        </div>
      </div>
    </>
  );

  return ReactDOM.createPortal(dialog, elRef.current);
};

export default Modal;
