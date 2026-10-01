import type { Message } from "@/domain/Message";

export interface IMessageRepository {
  save(senderEmail: string, content: string): Promise<Message>;
  getById(id: string): Promise<Message>;
}
