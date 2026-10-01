export type MessageStatus =
  | "Pending"
  | "Sent"
  | "SentUrgent"
  | "RejectedSpam"
  | "RejectedThreat"
  | "RejectedReview";

export interface Message {
  id: string;
  status: MessageStatus;
  senderEmail: string;
  createdAt: string;
  confidence: number | null;       // 0-1, ej. 0.87 = 87%
  probabilities: string | null;    // JSON: {"spam":0.02,"normal":0.87,...}
}
