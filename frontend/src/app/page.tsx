"use client";

import { useState, useEffect, useCallback } from "react";
import { messageRepository } from "@/container";
import { submitMessage } from "@/application/useCases/submitMessage";
import { getMessageStatus } from "@/application/useCases/getMessageStatus";
import { StatusBadge } from "@/components/StatusBadge";
import { ConfidenceBar } from "@/components/ConfidenceBar";
import type { Message } from "@/domain/Message";

const TERMINAL_STATUSES = ["Sent", "SentUrgent", "RejectedSpam", "RejectedThreat", "RejectedReview"];

export default function Home() {
  const [email, setEmail] = useState("");
  const [content, setContent] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);

  const pollStatuses = useCallback(async () => {
    const pending = messages.filter((m) => !TERMINAL_STATUSES.includes(m.status));
    if (pending.length === 0) return;
    const updated = await Promise.all(
      pending.map((m) => getMessageStatus(messageRepository, m.id).catch(() => m))
    );
    setMessages((prev) =>
      prev.map((m) => updated.find((u) => u.id === m.id) ?? m)
    );
  }, [messages]);

  useEffect(() => {
    const interval = setInterval(pollStatuses, 3000);
    return () => clearInterval(interval);
  }, [pollStatuses]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const message = await submitMessage(messageRepository, email, content);
      setMessages((prev) => [message, ...prev]);
      setContent("");
    } catch {
      setError("No se pudo enviar el mensaje. ¿Está la API corriendo?");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="min-h-screen bg-[#0a0a0a] py-12 px-4">
      <div className="max-w-xl mx-auto space-y-8">

        {/* Header */}
        <div className="text-center">
          <div className="inline-flex items-center gap-2 mb-3">
            <div className="w-2 h-6 bg-red-600 rounded-sm" />
            <h1 className="text-3xl font-bold tracking-tight text-white">MessageGuard</h1>
          </div>
          <p className="text-sm text-neutral-500">
            Sistema de mensajería con clasificación inteligente
          </p>
        </div>

        {/* Form */}
        <form
          onSubmit={handleSubmit}
          className="bg-[#161616] border border-[#2a2a2a] rounded-2xl p-6 space-y-4 shadow-xl"
        >
          <div>
            <label className="block text-xs font-semibold text-neutral-400 uppercase tracking-widest mb-1.5">
              Tu email
            </label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="usuario@ejemplo.com"
              className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2.5 text-sm text-white placeholder-neutral-600 focus:outline-none focus:border-red-600 focus:ring-1 focus:ring-red-600 transition"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-neutral-400 uppercase tracking-widest mb-1.5">
              Mensaje
            </label>
            <textarea
              required
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Escribe tu mensaje aquí..."
              rows={4}
              className="w-full bg-[#0f0f0f] border border-[#2a2a2a] rounded-lg px-3 py-2.5 text-sm text-white placeholder-neutral-600 focus:outline-none focus:border-red-600 focus:ring-1 focus:ring-red-600 resize-none transition"
            />
          </div>

          {error && (
            <p className="text-xs text-red-400 bg-red-950/40 border border-red-900/50 rounded-lg px-3 py-2">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="w-full bg-red-600 hover:bg-red-700 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold rounded-lg py-2.5 text-sm transition-colors"
          >
            {submitting ? "Enviando..." : "Enviar mensaje"}
          </button>
        </form>

        {/* Messages list */}
        {messages.length > 0 && (
          <div className="space-y-3">
            <p className="text-xs font-semibold text-neutral-500 uppercase tracking-widest px-1">
              Mensajes enviados
            </p>

            {messages.map((m) => (
              <div
                key={m.id}
                className="bg-[#161616] border border-[#2a2a2a] rounded-2xl px-5 py-4 space-y-3 shadow-lg"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-white truncate">{m.senderEmail}</p>
                    <p className="text-xs text-neutral-600 mt-0.5 font-mono truncate">{m.id}</p>
                    <p className="text-xs text-neutral-600 mt-0.5">
                      {new Date(m.createdAt).toLocaleTimeString()}
                    </p>
                  </div>
                  <div className="shrink-0">
                    <StatusBadge status={m.status} />
                  </div>
                </div>

                {m.confidence != null && (
                  <ConfidenceBar confidence={m.confidence} probabilities={m.probabilities} />
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </main>
  );
}
