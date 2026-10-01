import type { MessageStatus } from "@/domain/Message";

const config: Record<MessageStatus, { label: string; className: string }> = {
  Pending:        { label: "Pendiente",          className: "bg-neutral-800 text-neutral-300 border border-neutral-700" },
  Sent:           { label: "Enviado",             className: "bg-emerald-950 text-emerald-400 border border-emerald-800" },
  SentUrgent:     { label: "Enviado urgente",     className: "bg-orange-950 text-orange-400 border border-orange-800" },
  RejectedSpam:   { label: "Rechazado (spam)",    className: "bg-yellow-950 text-yellow-400 border border-yellow-800" },
  RejectedThreat: { label: "Rechazado (amenaza)", className: "bg-red-950 text-red-400 border border-red-800" },
  RejectedReview: { label: "En revisión",         className: "bg-purple-950 text-purple-400 border border-purple-800" },
};

export function StatusBadge({ status }: { status: MessageStatus }) {
  const { label, className } = config[status] ?? { label: status, className: "bg-neutral-800 text-neutral-400 border border-neutral-700" };
  return (
    <span className={`inline-block rounded-full px-3 py-1 text-xs font-semibold whitespace-nowrap ${className}`}>
      {label}
    </span>
  );
}
