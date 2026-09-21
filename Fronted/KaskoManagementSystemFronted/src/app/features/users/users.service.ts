import { environment } from '../../../environments/environment';
import { Injectable, inject } from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  User,
  UserCreateDto,
  UserUpdateDto
} from './user';


@Injectable({
  providedIn: 'root'
})
export class UserService {

  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiBaseUrl}/User`;


  getAll(): Observable<User[]> {

    return this.http.get<User[]>(
      this.apiUrl
    );

  }


  getById(
    id: string
  ): Observable<User> {

    return this.http.get<User>(
      `${this.apiUrl}/${id}`
    );

  }


  create(
    dto: UserCreateDto
  ): Observable<User> {

    return this.http.post<User>(
      this.apiUrl,
      dto
    );

  }


  update(
  dto: UserUpdateDto
): Observable<void> {

  return this.http.put<void>(
    this.apiUrl,
    dto
  );

}


  delete(id: string) {
  return this.http.delete(
    `${this.apiUrl}/${id}`
  );
}
  }