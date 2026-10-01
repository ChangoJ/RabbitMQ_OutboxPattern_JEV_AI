import type { IMessageRepository } from "@/application/ports/IMessageRepository";
import type { Message } from "@/domain/Message";

export async function getMessageStatus(
  repo: IMessageRepository,
  id: string
): Promise<Message> {
  return repo.getById(id);
}
