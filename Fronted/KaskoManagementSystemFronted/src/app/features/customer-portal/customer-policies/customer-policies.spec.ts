import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerPolicies } from './customer-policies';

describe('CustomerPolicies', () => {
  let component: CustomerPolicies;
  let fixture: ComponentFixture<CustomerPolicies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerPolicies],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerPolicies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
