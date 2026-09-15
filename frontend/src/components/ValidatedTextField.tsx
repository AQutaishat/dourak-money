import { useCallback, useMemo, useState } from "react";
import { TextField, type TextFieldProps } from "@mui/material";
import { useTranslation } from "react-i18next";

/**
 * prompt02 §Login screen defines one **common validation pattern** for every form with
 * required/format-checked fields (login, register, create-circle, profile):
 *
 *   on blur → if empty-when-required or format-invalid, show a red border + inline error text.
 *
 * It lives here once rather than being re-implemented per screen. MUI already paints the border
 * red from `error`, so a field only has to say *which* rules apply.
 */
export type FieldRule = "required" | "email" | "password";

export const PASSWORD_MIN_LENGTH = 8;

export interface FieldState {
  value: string;
  error: string | null;
  touched: boolean;
}

function validate(value: string, rules: FieldRule[]): "required" | "email" | "password" | null {
  const trimmed = value.trim();
  if (rules.includes("required") && trimmed.length === 0) return "required";
  if (trimmed.length === 0) return null; // Optional and empty — nothing else to check.
  if (rules.includes("email") && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)) return "email";
  // Kept in line with the backend Identity password options (8+, a letter and a digit).
  if (rules.includes("password") && !(value.length >= PASSWORD_MIN_LENGTH && /[A-Za-z]/.test(value) && /\d/.test(value)))
    return "password";
  return null;
}

/**
 * Hook form-state for one field. Errors appear on blur (and on submit), never while the user
 * is still typing their first character — but once a field is showing an error, correcting it
 * clears the error immediately, which is what makes the pattern feel helpful rather than nagging.
 */
export function useValidatedField(initial = "", rules: FieldRule[] = []) {
  const { t } = useTranslation();
  const [value, setValue] = useState(initial);
  const [touched, setTouched] = useState(false);

  const errorKey = useMemo(() => validate(value, rules), [value, rules]);
  const message = errorKey ? t(`validation.${errorKey}`) : null;

  const showError = touched && !!errorKey;

  const validateNow = useCallback(() => {
    setTouched(true);
    return !errorKey;
  }, [errorKey]);

  return {
    value,
    setValue,
    isValid: !errorKey,
    validateNow,
    reset: () => { setValue(initial); setTouched(false); },
    /** Spread onto a MUI TextField to get the whole pattern. */
    fieldProps: {
      value,
      onChange: (e: React.ChangeEvent<HTMLInputElement>) => setValue(e.target.value),
      onBlur: () => setTouched(true),
      error: showError,
      helperText: showError ? message : undefined,
    },
  };
}

/** A TextField that selects its content on focus — used for the contribution amount (§Create Circle). */
export function SelectOnFocusTextField(props: TextFieldProps) {
  return (
    <TextField
      {...props}
      onFocus={(e) => {
        // Fixes the "always has a leading zero" annoyance: typing replaces the default value.
        (e.target as HTMLInputElement).select();
        props.onFocus?.(e);
      }}
    />
  );
}
