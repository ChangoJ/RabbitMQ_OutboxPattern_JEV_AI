import { HttpMessageRepository } from "@/infrastructure/HttpMessageRepository";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5055";

export const messageRepository = new HttpMessageRepository(apiUrl);
