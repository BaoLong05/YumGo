export type Role =
  | 'Customer'
  | 'Driver'
  | 'RestaurantStaff'
  | 'RestaurantAdmin'
  | 'SystemStaff'
  | 'SystemAdmin';

export interface AuthUser {
  id: string;
  roles: Role[];
  permissions: string[];
}