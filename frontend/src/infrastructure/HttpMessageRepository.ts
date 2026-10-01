import type { IMessageRepository } from "@/application/ports/IMessageRepository";
import type { Message } from "@/domain/Message";

export class HttpMessageRepository implements IMessageRepository {
  constructor(private readonly baseUrl: string) {}

  async save(senderEmail: string, content: string): Promise<Message> {
    const res = await fetch(`${this.baseUrl}/api/messages`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ senderEmail, content }),
    });
    if (!res.ok) throw new Error("Error al enviar el mensaje");
    return res.json();
  }

  async getById(id: string): Promise<Message> {
    const res = await fetch(`${this.baseUrl}/api/messages/${id}`);
    if (!res.ok) throw new Error("Mensaje no encontrado");
    return res.json();
  }
}
