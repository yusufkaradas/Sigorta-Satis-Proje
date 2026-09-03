import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerEdit } from './customer-edit';

describe('CustomerEdit', () => {
  let component: CustomerEdit;
  let fixture: ComponentFixture<CustomerEdit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerEdit],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerEdit);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
