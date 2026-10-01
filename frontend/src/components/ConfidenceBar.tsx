"use client";

const LABEL: Record<string, string> = {
  spam: "Spam",
  threat: "Amenaza",
  urgent: "Urgente",
  normal: "Normal",
};

const COLOR: Record<string, string> = {
  spam:   "bg-yellow-500",
  threat: "bg-red-600",
  urgent: "bg-orange-500",
  normal: "bg-emerald-500",
};

interface Props {
  confidence: number;
  probabilities: string | null;
}

export function ConfidenceBar({ confidence, probabilities }: Props) {
  const probs: Record<string, number> = probabilities ? JSON.parse(probabilities) : {};

  // Ordena de mayor a menor probabilidad
  const sorted = Object.entries(probs).sort(([, a], [, b]) => b - a);

  return (
    <div className="space-y-2 pt-3 border-t border-[#2a2a2a]">
      <p className="text-xs text-neutral-500">
        Confianza:{" "}
        <span className="text-white font-semibold">{(confidence * 100).toFixed(0)}%</span>
      </p>
      {sorted.map(([key, val]) => (
        <div key={key} className="flex items-center gap-3">
          <span className="w-14 shrink-0 text-xs text-neutral-500">{LABEL[key] ?? key}</span>
          <div className="flex-1 bg-[#222] rounded-full h-1.5 overflow-hidden">
            <div
              className={`${COLOR[key] ?? "bg-red-600"} h-1.5 rounded-full transition-all duration-500`}
              style={{ width: `${(val * 100).toFixed(1)}%` }}
            />
          </div>
          <span className="w-8 text-right text-xs text-neutral-500">
            {(val * 100).toFixed(0)}%
          </span>
        </div>
      ))}
    </div>
  );
}
