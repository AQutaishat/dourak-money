import { apiClient } from "./client";

export const supportApi = {
  /** Public — no auth required, so a locked-out user can still reach out. */
  submit: (data: { name?: string; email: string; message: string; attachment?: File | null }) => {
    const form = new FormData();
    if (data.name) form.append("name", data.name);
    form.append("email", data.email);
    form.append("message", data.message);
    if (data.attachment) form.append("attachment", data.attachment);
    return apiClient.post<{ id: number }>("/support", form).then((r) => r.data);
  },
};
