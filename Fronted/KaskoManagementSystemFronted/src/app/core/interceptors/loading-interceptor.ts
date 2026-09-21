import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has('X-Silent-Error') || req.headers.has('X-No-Loader') || req.url.includes('/Notification')) {
    return next(req);
  }

  const loading = inject(LoadingService);

  loading.start();

  return next(req).pipe(finalize(() => loading.stop()));
};
