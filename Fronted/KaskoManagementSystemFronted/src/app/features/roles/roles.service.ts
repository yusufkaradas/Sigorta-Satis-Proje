import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Role {
  id: string;
  name: string;
}

export interface RoleUpdateDto {
  id: string;
  name: string;
}
export interface RoleCreateDto {
  name: string;
}

@Injectable({
  providedIn: 'root'
})

export class RolesService {

  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7086/api/Role';


  getAll(): Observable<Role[]> {
    return this.http.get<Role[]>(
      this.apiUrl
    );
  }


  getById(id: string): Observable<Role> {
    return this.http.get<Role>(
      `${this.apiUrl}/${id}`
    );
  }


  create(
    dto: RoleCreateDto
  ): Observable<Role> {

    return this.http.post<Role>(
      this.apiUrl,
      dto
    );
  }
  update(
  dto: RoleUpdateDto
): Observable<void> {

  return this.http.put<void>(
    this.apiUrl,
    dto
  );
}

delete(id: string): Observable<void> {

  return this.http.delete<void>(
    `${this.apiUrl}/${id}`
  );
  
}
}