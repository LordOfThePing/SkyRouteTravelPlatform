import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 0) {
        return throwError(() => new Error('Could not reach API. Check backend status and try again.'));
      }

      const body = (error.error ?? {}) as ProblemDetails;
      const validationError = firstValidationError(body);
      const message = validationError ?? body.detail ?? body.title ?? `Request failed (${error.status}).`;

      return throwError(() => new Error(message));
    }),
  );

function firstValidationError(problem: ProblemDetails): string | null {
  if (!problem.errors) {
    return null;
  }

  for (const messages of Object.values(problem.errors)) {
    if (messages.length > 0) {
      return messages[0];
    }
  }

  return null;
}
