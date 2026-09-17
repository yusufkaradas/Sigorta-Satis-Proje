export interface User {

  id: string;

  firstName: string;

  lastName: string;

  email: string;

  phoneNumber?: string | null;

  roleId: string;

  roleName?: string | null;

  isActive: boolean;

  createdDate?: string;

  updatedDate?: string | null;
}


export interface UserCreateDto {

  customerId?: string | null;

  identityNumber?: string | null;

  dateOfBirth?: string | null;

  city?: string | null;

  district?: string | null;

  address?: string | null;


  firstName: string;

  lastName: string;

  email: string;

  password: string;

  phoneNumber?: string | null;

  roleId: string;
}


export interface UserUpdateDto {

  id: string;

  firstName: string;

  lastName: string;

  email: string;

  phoneNumber?: string | null;

  roleId: string;

  isActive: boolean;
}