import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerPayments } from './customer-payments';

describe('CustomerPayments', () => {
  let component: CustomerPayments;
  let fixture: ComponentFixture<CustomerPayments>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerPayments],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerPayments);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
