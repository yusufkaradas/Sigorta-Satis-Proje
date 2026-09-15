import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerQuotes } from './customer-quotes';

describe('CustomerQuotes', () => {
  let component: CustomerQuotes;
  let fixture: ComponentFixture<CustomerQuotes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerQuotes],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerQuotes);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
