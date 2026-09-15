import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';

import {
  QuickQuote
} from './features/quick-quotes/quick-quote/quick-quote';

import {
  QuickQuoteStart
} from './features/quick-quotes/quick-quote-start/quick-quote-start';

import {
  Dashboard
} from './features/dashboard/dashboard';

import {
  Payments
} from './features/payments/payments';

import {
  Customers
} from './features/customers/customer';

import {
  CustomerDetail
} from './features/customers/customer-detail/customer-detail';

import {
  CustomerCreate
} from './features/customers/customer-create/customer-create';

import {
  CustomerEdit
} from './features/customers/customer-edit/customer-edit';

import {
  Vehicle
} from './features/vehicles/vehicle';

import {
  VehicleCreate
} from './features/vehicles/vehicle-create/vehicle-create';

import {
  VehicleEdit
} from './features/vehicles/vehicle-edit/vehicle-edit';

import {
  VehicleDetail
} from './features/vehicles/vehicle-detail/vehicle-detail';

import {
  Policies
} from './features/policies/policies';

import {
  PolicyCreate
} from './features/policies/policy-create/policy-create';

import {
  PolicyDetail
} from './features/policies/policy-detail/policy-detail';

import {
  PolicyEdit
} from './features/policies/policy-edit/policy-edit';

import {
  Quotes
} from './features/quotes/quote';

import {
  QuoteDetail
} from './features/quotes/quote-detail/quote-detail';

import {
  QuoteCreate
} from './features/quotes/quote-create/quote-create';

import {
  QuoteEdit
} from './features/quotes/quote-edit/quote-edit';

import {
  PaymentDetail
} from './features/payments/payment-detail';

import {
  PaymentCreate
} from './features/payments/payment-create/payment-create';

import {
  Users
} from './features/users/users';

import {
  UserDetail
} from './features/users/user-detail/user-detail';

import {
  UserCreate
} from './features/users/user-create/user-create';

import {
  UserEdit
} from './features/users/user-edit/user-edit';

import {
  Roles
} from './features/roles/roles';

import {
  RoleCreate
} from './features/roles/role-create/role-create';

import {
  RoleDetail
} from './features/roles/role-detail/role-detail';

import {
  RoleEdit
} from './features/roles/role-edit/role-edit';

import {
  Layout
} from './features/layout/layout';

import {
  Notifications
} from './features/notifications/notifications/notifications';

import {
  Register
} from './features/auth/register/register';

import {
  CustomerDashboard
} from './features/customer-portal/customer-dashboard/customer-dashboard';
import {
  roleGuard
} from './core/guards/role-guards';

import {
  CustomerLayout
} from './features/customer-portal/customer-layout/customer-layout';
import {
  CustomerVehicles
} from './features/customer-portal/customer-vehicles/customer-vehicles';

import {
  CustomerQuotes
} from './features/customer-portal/customer-quotes/customer-quotes';

import {
  CustomerPolicies
} from './features/customer-portal/customer-policies/customer-policies';

import {
  CustomerPayments
} from './features/customer-portal/customer-payments/customer-payments';

import { authGuard } from './core/guards/auth-guard';

import {
  ForgotPassword
} from './features/auth/forgot-password/forgot-password';

import {
  PricingRules
} from './features/pricing/pricing-rules/pricing-rules';

import {
  PricingRequests
} from './features/pricing/pricing-requests/pricing-requests';


export const routes: Routes = [

  {
    path: '',
    component: QuickQuote,
    pathMatch: 'full'
  },

  {
    path: 'quick-quote',
    component: QuickQuote
  },

  {
    path: 'quick-quote/start',
    component: QuickQuoteStart
  },

  {
    path: 'login',
    component: Login
  },

  {
    path: 'register',
    component: Register
  },

  {
    path: 'forgot-password',
    component: ForgotPassword
  },
 {
  path: 'customer',
  component: CustomerLayout,
  canActivate: [authGuard, roleGuard],
  data: {
    roles: ['Customer']
  },
  children: [
    {
      path: '',
      component: CustomerDashboard
    },
    {
      path: 'vehicles',
      component: CustomerVehicles
    },
    {
      path: 'vehicles/:id',
      component: VehicleDetail,
      data: {
        mode: 'customer'
      }
    },
    {
      path: 'quotes',
      component: CustomerQuotes
    },
    {
      path: 'quotes/new',
      component: QuoteCreate,
      data: {
        mode: 'customer'
      }
    },
    {
      path: 'quotes/:id',
      component: QuoteDetail,
      data: {
        mode: 'customer'
      }
    },
    {
      path: 'policies',
      component: CustomerPolicies
    },
    {
      path: 'policies/:id',
      component: PolicyDetail,
      data: {
        mode: 'customer'
      }
    },
    {
      path: 'payments',
      component: CustomerPayments
    },
    {
      path: 'payments/:id',
      component: PaymentDetail,
      data: {
        mode: 'customer'
      }
    }
  ]
},
  {
    path: 'manager',
    component: Layout,
    canActivate: [authGuard, roleGuard],
    data: {
      roles: ['Manager'],
      portal: 'manager'
    },
    children: [
      { path: '', component: Dashboard, data: { mode: 'manager' } },
      { path: 'customers', component: Customers, data: { mode: 'manager' } },
      { path: 'customers/:id', component: CustomerDetail, data: { mode: 'manager' } },
      { path: 'vehicles', component: Vehicle, data: { mode: 'manager' } },
      { path: 'vehicles/:id', component: VehicleDetail, data: { mode: 'manager' } },
      { path: 'quotes', component: Quotes, data: { mode: 'manager' } },
      { path: 'quotes/:id', component: QuoteDetail, data: { mode: 'manager' } },
      { path: 'policies', component: Policies, data: { mode: 'manager' } },
      { path: 'policies/:id', component: PolicyDetail, data: { mode: 'manager' } },
      { path: 'payments', component: Payments, data: { mode: 'manager' } },
      { path: 'payments/:id', component: PaymentDetail, data: { mode: 'manager' } },
      { path: 'pricing-rules', component: PricingRules, data: { mode: 'manager' } },
      { path: 'pricing-requests', component: PricingRequests, data: { mode: 'manager' } }
    ]
  },

  {
    path: '',
    component: Layout,
    canActivate: [authGuard, roleGuard],
    data: {
      roles: ['Admin'],
      portal: 'admin'
    },

    children: [

      {
        path: 'pricing-requests',
        component: PricingRequests
      },

      {
        path: 'dashboard',
        component: Dashboard
      },

      {
        path: 'customers',
        component: Customers
      },

      {
        path: 'customers/new',
        component: CustomerCreate
      },

      {
        path: 'customers/:id/edit',
        component: CustomerEdit
      },

      {
        path: 'customers/:id',
        component: CustomerDetail
      },


      {
        path: 'vehicles',
        component: Vehicle
      },

      {
        path: 'vehicles/new',
        component: VehicleCreate
      },

      {
        path: 'vehicles/:id/edit',
        component: VehicleEdit
      },

      {
        path: 'vehicles/:id',
        component: VehicleDetail
      },


      {
        path: 'policies',
        component: Policies
      },

      {
        path: 'policies/new',
        component: PolicyCreate
      },

      {
        path: 'policies/:id/edit',
        component: PolicyEdit
      },

      {
        path: 'policies/:id',
        component: PolicyDetail
      },


      {
        path: 'quotes',
        component: Quotes
      },

      {
        path: 'quotes/new',
        component: QuoteCreate
      },

      {
        path: 'quotes/:id/edit',
        component: QuoteEdit
      },

      {
        path: 'quotes/:id',
        component: QuoteDetail
      },


      {
        path: 'payments',
        component: Payments
      },

      {
        path: 'payments/new',
        component: PaymentCreate
      },

      {
        path: 'payments/:id',
        component: PaymentDetail
      },


      {
        path: 'notifications',
        component: Notifications,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Customer']
        }
      },


      {
        path: 'users',
        component: Users,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'users/new',
        component: UserCreate,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'users/:id/edit',
        component: UserEdit,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'users/:id',
        component: UserDetail,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },


      {
        path: 'roles',
        component: Roles,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'roles/new',
        component: RoleCreate,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'roles/:id/edit',
        component: RoleEdit,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },

      {
        path: 'roles/:id',
        component: RoleDetail,
        canActivate: [authGuard, roleGuard],
        data: {
          roles: ['Admin']
        }
      },


      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }

    ]
  },


  {
    path: '**',
    redirectTo: ''
  }

];