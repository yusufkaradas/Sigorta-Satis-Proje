import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerVehicles } from './customer-vehicles';

describe('CustomerVehicles', () => {
  let component: CustomerVehicles;
  let fixture: ComponentFixture<CustomerVehicles>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerVehicles],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerVehicles);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
