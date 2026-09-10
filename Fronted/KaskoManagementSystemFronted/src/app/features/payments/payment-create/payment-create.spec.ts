import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaymentCreate } from './payment-create';

describe('PaymentCreate', () => {
  let component: PaymentCreate;
  let fixture: ComponentFixture<PaymentCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentCreate],
    }).compileComponents();

    fixture = TestBed.createComponent(PaymentCreate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
