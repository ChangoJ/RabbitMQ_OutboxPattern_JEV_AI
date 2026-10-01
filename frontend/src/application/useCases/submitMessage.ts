import type { IMessageRepository } from "@/application/ports/IMessageRepository";
import type { Message } from "@/domain/Message";

export async function submitMessage(
  repo: IMessageRepository,
  senderEmail: string,
  content: string
): Promise<Message> {
  return repo.save(senderEmail, content);
}
